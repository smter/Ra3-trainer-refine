# Implement: 填充 Trellis Spec

## Execution Order

### Step 1: 删除旧目录 & 创建新目录
```bash
rm -rf .trellis/spec/backend
rm -rf .trellis/spec/frontend
mkdir -p .trellis/spec/core
mkdir -p .trellis/spec/app
```

### Step 2: 写入 Core 层 spec（5 个文件，按依赖顺序）

| # | 文件 | 依赖 |
|---|------|------|
| 2.1 | `core/index.md` | 无 |
| 2.2 | `core/directory-structure.md` | 无 |
| 2.3 | `core/error-handling.md` | 无 |
| 2.4 | `core/quality-guidelines.md` | 无 |
| 2.5 | `core/logging-guidelines.md` | 无 |

### Step 3: 写入 App 层 spec（6 个文件，按依赖顺序）

| # | 文件 | 依赖 |
|---|------|------|
| 3.1 | `app/index.md` | 无 |
| 3.2 | `app/directory-structure.md` | 无 |
| 3.3 | `app/component-guidelines.md` | 无 |
| 3.4 | `app/state-management.md` | 无 |
| 3.5 | `app/hook-guidelines.md` | 无 |
| 3.6 | `app/type-safety.md` | 无 |
| 3.7 | `app/quality-guidelines.md` | 无 |

### Step 4: 更新 guides 索引
- 检查 `spec/guides/index.md` 交叉引用，确保不指向旧 `backend/` / `frontend/` 路径

### Step 5: 验证
```bash
# 确认旧目录已删除
test ! -d .trellis/spec/backend && echo "PASS: backend removed"
test ! -d .trellis/spec/frontend && echo "PASS: frontend removed"
# 确认新目录存在
test -d .trellis/spec/core && echo "PASS: core exists"
test -d .trellis/spec/app && echo "PASS: app exists"
# 确认 database-guidelines.md 已删除
test ! -f .trellis/spec/core/database-guidelines.md && echo "PASS: database-guidelines removed"
# 统计文件数
echo "Core files:" && ls .trellis/spec/core/ | wc -l
echo "App files:" && ls .trellis/spec/app/ | wc -l
```

### Step 6: 更新 check.jsonl / implement.jsonl
确保 task 的 jsonl manifest 引用新的 spec 文件路径

## Validation Commands
```bash
# 每个 spec 文件至少有 2 个代码块
for f in .trellis/spec/core/*.md .trellis/spec/app/*.md; do
  count=$(grep -c '```' "$f" || echo 0)
  echo "$f: $count code fences"
done

# 每个 spec 文件提到"禁止"
for f in .trellis/spec/core/*.md .trellis/spec/app/*.md; do
  echo "$f: $(grep -c '禁止\|Forbidden\|避免' "$f" || echo 0) forbidden mentions"
done
```

## Rollback
```bash
rm -rf .trellis/spec/core .trellis/spec/app
git checkout -- .trellis/spec/
```
