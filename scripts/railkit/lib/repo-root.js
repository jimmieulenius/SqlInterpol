'use strict';

const fs = require('fs');
const path = require('path');

/**
 * Resolve host/kit repo root whether scripts live at:
 * - <root>/scripts/<area>/ (merge layout)
 * - <root>/scripts/railkit/<area>/ (Quick Start nested layout)
 * Walks up until docs/ADOPTION.md + package.json exist.
 */
function findRepoRoot(startDir) {
  let dir = path.resolve(startDir);
  for (;;) {
    const adoption = path.join(dir, 'docs', 'ADOPTION.md');
    const pkg = path.join(dir, 'package.json');
    if (fs.existsSync(adoption) && fs.existsSync(pkg)) return dir;
    const parent = path.dirname(dir);
    if (parent === dir) break;
    dir = parent;
  }
  return path.resolve(startDir, '../..');
}

module.exports = { findRepoRoot };
