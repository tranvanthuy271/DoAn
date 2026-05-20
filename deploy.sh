#!/usr/bin/env bash
# =============================================================================
# deploy.sh  —  Kéo code mới nhất từ GitHub và rebuild GameServerApi trên VPS
# Chạy trên VPS Linux:  bash deploy.sh
# =============================================================================
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$REPO_DIR/docker-compose.yml"

# ── Màu sắc cho log ──────────────────────────────────────────────────────────
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
log_info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

# ── Kiểm tra file .env ───────────────────────────────────────────────────────
if [[ ! -f "$REPO_DIR/.env" ]]; then
  log_error "Không tìm thấy file .env !"
  echo "  Hãy copy và điền thông tin: cp .env.example .env && nano .env"
  exit 1
fi

log_info "=== Bắt đầu deploy tại: $(date) ==="

# ── 1. Pull code mới nhất ────────────────────────────────────────────────────
log_info "Đang git pull..."
cd "$REPO_DIR"
git pull --ff-only

# ── 2. Rebuild và restart GameServerApi (không làm ảnh hưởng DB) ─────────────
log_info "Đang rebuild container api..."
docker compose -f "$COMPOSE_FILE" up -d --build --no-deps api

# ── 3. Kiểm tra trạng thái ───────────────────────────────────────────────────
log_info "Chờ container khởi động..."
sleep 5
docker compose -f "$COMPOSE_FILE" ps

# ── 4. Dọn image cũ không còn dùng ──────────────────────────────────────────
log_info "Dọn dangling images..."
docker image prune -f --filter "dangling=true" > /dev/null

log_info "=== Deploy hoàn tất! ==="
echo ""
echo "  API:    http://$(hostname -I | awk '{print $1}'):5000"
echo "  Logs:   docker compose logs -f api"
echo "  Status: docker compose ps"
