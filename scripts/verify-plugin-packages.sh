#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

require_command() {
    local command_name="$1"

    if ! command -v "${command_name}" >/dev/null 2>&1; then
        echo "Required command not found: ${command_name}" >&2
        exit 1
    fi
}

read_manifest_property_from_zip() {
    local zip_path="$1"
    local property_name="$2"

    unzip -p "${zip_path}" plugin.json | awk -F'"' -v property_name="${property_name}" '$2 == property_name { print $4; exit }'
}

validate_package() {
    local package_path="$1"
    local package_directory
    local package_name
    local plugin_manifest_count
    local plugin_assembly
    local config_schema
    local system_name
    local plugin_version
    local expected_package_name

    package_directory="$(dirname "${package_path}")"
    package_name="$(basename "${package_path}")"

    plugin_manifest_count="$(unzip -Z1 "${package_path}" | grep -c '^plugin.json$' || true)"
    if [[ "${plugin_manifest_count}" != "1" ]]; then
        echo "Package ${package_path} must contain exactly one plugin.json." >&2
        exit 1
    fi

    system_name="$(read_manifest_property_from_zip "${package_path}" "systemName")"
    plugin_version="$(read_manifest_property_from_zip "${package_path}" "version")"
    plugin_assembly="$(read_manifest_property_from_zip "${package_path}" "assembly")"
    config_schema="$(read_manifest_property_from_zip "${package_path}" "configSchema")"
    if [[ -z "${system_name}" || -z "${plugin_version}" || -z "${plugin_assembly}" ]]; then
        echo "Package ${package_path} has an invalid plugin manifest." >&2
        exit 1
    fi

    expected_package_name="${system_name}-${plugin_version}.zip"
    if [[ "${package_name}" != "${expected_package_name}" ]]; then
        echo "Package ${package_path} must be named ${expected_package_name}." >&2
        exit 1
    fi

    if [[ "${package_directory}" != "${REPO_ROOT}/artifacts/plugins/${system_name}" ]]; then
        echo "Package ${package_path} is stored in an unexpected directory." >&2
        exit 1
    fi

    if ! unzip -Z1 "${package_path}" | grep -Fqx "${plugin_assembly}"; then
        echo "Package ${package_path} does not contain the plugin assembly ${plugin_assembly}." >&2
        exit 1
    fi

    if ! unzip -Z1 "${package_path}" | grep -q '\.deps\.json$'; then
        echo "Package ${package_path} does not contain a .deps.json file." >&2
        exit 1
    fi

    if ! unzip -Z1 "${package_path}" | grep -q '\.runtimeconfig\.json$'; then
        echo "Package ${package_path} does not contain a .runtimeconfig.json file." >&2
        exit 1
    fi

    if [[ -n "${config_schema}" ]] && ! unzip -Z1 "${package_path}" | grep -Fqx "${config_schema}"; then
        echo "Package ${package_path} declares configSchema ${config_schema}, but the file is missing." >&2
        exit 1
    fi
}

require_command unzip

"${REPO_ROOT}/scripts/package-plugins.sh"

shopt -s nullglob
plugin_directories=("${REPO_ROOT}"/artifacts/plugins/*)

if ((${#plugin_directories[@]} == 0)); then
    echo "No plugin packages were generated." >&2
    exit 1
fi

for plugin_directory in "${plugin_directories[@]}"; do
    if [[ ! -d "${plugin_directory}" ]]; then
        continue
    fi

    packages=("${plugin_directory}"/*.zip)
    if ((${#packages[@]} != 1)); then
        echo "Plugin directory ${plugin_directory} must contain exactly one .zip package." >&2
        exit 1
    fi

    validate_package "${packages[0]}"
done

echo "All plugin packages passed validation."
