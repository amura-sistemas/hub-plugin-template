#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PACKAGE_ID="Amura.Hub.Plugin"
PROPERTY_NAME="AmuraHubPluginSdkVersion"
NUGET_SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

require_command() {
    local command_name="$1"

    if ! command -v "${command_name}" >/dev/null 2>&1; then
        echo "Required command not found: ${command_name}" >&2
        exit 1
    fi
}

read_current_version() {
    sed -n "s:.*<${PROPERTY_NAME}>\\(.*\\)</${PROPERTY_NAME}>.*:\\1:p" \
        "${REPO_ROOT}/Directory.Build.props" |
        head -n 1 |
        tr -d '[:space:]'
}

read_latest_version() {
    dotnet package search "${PACKAGE_ID}" \
        --source "${NUGET_SOURCE}" \
        --exact-match |
        awk -F'|' -v package_id="${PACKAGE_ID}" '
            NF >= 4 {
                package = $2
                version = $3
                gsub(/^[[:space:]]+|[[:space:]]+$/, "", package)
                gsub(/^[[:space:]]+|[[:space:]]+$/, "", version)
                if (package == package_id && version != "Version") {
                    print version
                }
            }
        ' |
        sort -V |
        tail -n 1
}

require_command dotnet
require_command sort

current_version="$(read_current_version)"
if [[ -z "${current_version}" ]]; then
    echo "Unable to read ${PROPERTY_NAME} from Directory.Build.props." >&2
    exit 1
fi

latest_version="$(read_latest_version)"
if [[ -z "${latest_version}" ]]; then
    echo "Unable to find ${PACKAGE_ID} in ${NUGET_SOURCE}." >&2
    exit 1
fi

highest_version="$(printf '%s\n%s\n' "${current_version}" "${latest_version}" | sort -V | tail -n 1)"

echo "${PACKAGE_ID} current: ${current_version}"
echo "${PACKAGE_ID} latest:  ${latest_version}"

if [[ "${highest_version}" != "${current_version}" ]]; then
    echo "Update Directory.Build.props to ${latest_version} before distributing plugins." >&2
    exit 1
fi

echo "SDK package is up to date."
