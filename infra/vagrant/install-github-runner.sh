#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run this script with sudo or as root." >&2
  exit 1
fi

RUNNER_URL="${RUNNER_URL:-}"
RUNNER_TOKEN="${RUNNER_TOKEN:-}"
RUNNER_NAME="${RUNNER_NAME:-labapi-dev-runner}"
RUNNER_LABELS="${RUNNER_LABELS:-self-hosted,linux,x64,dev}"
RUNNER_USER="${RUNNER_USER:-github-runner}"
RUNNER_ROOT="${RUNNER_ROOT:-/opt/actions-runner}"
RUNNER_DIR="${RUNNER_ROOT}/${RUNNER_NAME}"

if [[ -z "${RUNNER_URL}" || -z "${RUNNER_TOKEN}" ]]; then
  cat >&2 <<'EOF'
Missing RUNNER_URL or RUNNER_TOKEN.

Example:
  sudo RUNNER_URL="https://github.com/owner/repo" \
    RUNNER_TOKEN="ghr_xxx" \
    ./infra/vagrant/install-github-runner.sh
EOF
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive
apt-get update
ICU_PACKAGE="$(apt-cache pkgnames 'libicu*' | sort -V | tail -n 1)"
if [[ -z "${ICU_PACKAGE}" ]]; then
  ICU_PACKAGE="libicu72"
fi

apt-get install -y ca-certificates curl jq tar unzip git "${ICU_PACKAGE}"

if ! id -u "${RUNNER_USER}" >/dev/null 2>&1; then
  useradd --create-home --shell /bin/bash "${RUNNER_USER}"
fi

usermod -aG docker "${RUNNER_USER}" || true

mkdir -p "${RUNNER_ROOT}"
if [[ -d "${RUNNER_DIR}" ]]; then
  pushd "${RUNNER_DIR}" >/dev/null
  if [[ -x ./svc.sh ]]; then
    ./svc.sh stop || true
    ./svc.sh uninstall || true
  fi

  if [[ -x ./config.sh && -f .runner ]]; then
    sudo -u "${RUNNER_USER}" ./config.sh remove --token "${RUNNER_TOKEN}" || true
  fi
  popd >/dev/null

  rm -rf "${RUNNER_DIR}"
fi

mkdir -p "${RUNNER_DIR}"

RUNNER_VERSION="${RUNNER_VERSION:-$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest | jq -r '.tag_name | ltrimstr("v")')}"
RUNNER_ARCHIVE="actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz"
RUNNER_DOWNLOAD_URL="https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${RUNNER_ARCHIVE}"

curl -fsSL "${RUNNER_DOWNLOAD_URL}" -o "/tmp/${RUNNER_ARCHIVE}"
tar -xzf "/tmp/${RUNNER_ARCHIVE}" -C "${RUNNER_DIR}"
chown -R "${RUNNER_USER}:${RUNNER_USER}" "${RUNNER_DIR}"

pushd "${RUNNER_DIR}" >/dev/null
sudo -u "${RUNNER_USER}" ./config.sh \
  --url "${RUNNER_URL}" \
  --token "${RUNNER_TOKEN}" \
  --name "${RUNNER_NAME}" \
  --labels "${RUNNER_LABELS}" \
  --unattended \
  --replace \
  --work "_work"

./svc.sh install "${RUNNER_USER}"
./svc.sh start
popd >/dev/null

echo "Runner installed at ${RUNNER_DIR}"
echo "Runner name: ${RUNNER_NAME}"
echo "Runner labels: ${RUNNER_LABELS}"