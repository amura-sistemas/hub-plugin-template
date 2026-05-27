#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"
SOLUTION_FILE="${SOLUTION_FILE:-HubPluginTemplate.slnx}"
NO_BUILD="false"

if [[ "${SOLUTION_FILE}" != /* ]]; then
    SOLUTION_FILE="${REPO_ROOT}/${SOLUTION_FILE}"
fi

for arg in "$@"; do
    case "${arg}" in
        --no-build)
            NO_BUILD="true"
            ;;
        *)
            echo "Unknown argument: ${arg}" >&2
            exit 1
            ;;
    esac
done

require_command() {
    local command_name="$1"

    if ! command -v "${command_name}" >/dev/null 2>&1; then
        echo "Required command not found: ${command_name}" >&2
        exit 1
    fi
}

read_manifest_property() {
    local manifest_path="$1"
    local property_name="$2"

    awk -F'"' -v property_name="${property_name}" '$2 == property_name { print $4; exit }' "${manifest_path}"
}

copy_stage_files() {
    local source_directory="$1"
    local destination_directory="$2"
    local plugin_assembly="$3"
    local plugin_assembly_base="${plugin_assembly%.dll}"

    mkdir -p "${destination_directory}"

    while IFS= read -r -d '' file_path; do
        local file_name
        file_name="$(basename "${file_path}")"

        if [[ "${file_name}" == "${plugin_assembly}" ]] || [[ "${file_name}" == "${plugin_assembly_base}".* ]]; then
            cp "${file_path}" "${destination_directory}/${file_name}"
            continue
        fi

        if [[ "${file_name}" == Amura.Hub.* ]] || [[ "${file_name}" == MediatR* ]] || [[ "${file_name}" == Microsoft.* ]]; then
            continue
        fi

        cp "${file_path}" "${destination_directory}/${file_name}"
    done < <(find "${source_directory}" -maxdepth 1 -type f -print0)
}

cleanup_removed_plugin_artifacts() {
    local -n active_plugins_ref="$1"

    if [[ ! -d "${REPO_ROOT}/artifacts/plugins" ]]; then
        return
    fi

    while IFS= read -r -d '' artifact_directory; do
        local artifact_name
        artifact_name="$(basename "${artifact_directory}")"

        if [[ -n "${active_plugins_ref[${artifact_name}]+x}" ]]; then
            continue
        fi

        rm -rf "${artifact_directory}"
    done < <(find "${REPO_ROOT}/artifacts/plugins" -mindepth 1 -maxdepth 1 -type d -print0)
}

require_command dotnet
require_command zip

if [[ "${NO_BUILD}" != "true" ]]; then
    if [[ ! -f "${SOLUTION_FILE}" ]]; then
        echo "Solution file not found: ${SOLUTION_FILE}" >&2
        exit 1
    fi

    dotnet build "${SOLUTION_FILE}" -c "${CONFIGURATION}"
fi

declare -a manifests=()
declare -A active_plugins=()

while IFS= read -r manifest_path; do
    manifests+=("${manifest_path}")

    system_name="$(read_manifest_property "${manifest_path}" "systemName")"
    if [[ -n "${system_name}" ]]; then
        active_plugins["${system_name}"]=1
    fi
done < <(find "${REPO_ROOT}/src/Plugins" -mindepth 2 -maxdepth 2 -name plugin.json | sort)

cleanup_removed_plugin_artifacts active_plugins

for manifest_path in "${manifests[@]}"; do
    system_name="$(read_manifest_property "${manifest_path}" "systemName")"
    plugin_version="$(read_manifest_property "${manifest_path}" "version")"
    plugin_assembly="$(read_manifest_property "${manifest_path}" "assembly")"

    if [[ -z "${system_name}" || -z "${plugin_version}" || -z "${plugin_assembly}" ]]; then
        echo "Invalid plugin manifest: ${manifest_path}" >&2
        exit 1
    fi

    stage_directory="${REPO_ROOT}/artifacts/plugins/${system_name}/stage"
    package_root="${REPO_ROOT}/artifacts/plugins/${system_name}"
    package_directory="${package_root}/package"
    zip_path="${package_root}/${system_name}-${plugin_version}.zip"

    if [[ ! -d "${stage_directory}" ]]; then
        echo "Stage directory not found for ${system_name}: ${stage_directory}" >&2
        exit 1
    fi

    find "${package_root}" -maxdepth 1 -type f -name '*.zip' -delete
    rm -rf "${package_directory}"
    mkdir -p "${package_directory}"

    copy_stage_files "${stage_directory}" "${package_directory}" "${plugin_assembly}"

    (
        cd "${package_directory}"
        zip -qr "${zip_path}" .
    )
done

echo "Plugin archives generated under ${REPO_ROOT}/artifacts/plugins"
