#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { findRepoRoot } = require('../lib/repo-root');

const root = findRepoRoot(__dirname);
const budgets = JSON.parse(
  fs.readFileSync(path.join(__dirname, 'architecture-budgets.json'), 'utf8')
);

function walk(dir, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  for (const name of fs.readdirSync(dir)) {
    if (name === 'node_modules' || name === '.git') continue;
    const p = path.join(dir, name);
    const st = fs.statSync(p);
    if (st.isDirectory()) walk(p, acc);
    else acc.push(p);
  }
  return acc;
}

const exts = new Set(budgets.extensions || ['.js', '.ts']);
const files = [];
for (const r of budgets.roots || ['src']) {
  const abs = path.join(root, r);
  for (const f of walk(abs)) {
    if (exts.has(path.extname(f))) files.push(f);
  }
}

const warn = budgets.warnLines || 400;
const max = budgets.maxLines || 800;
const overWarn = [];
const errors = [];
let largest = 0;

for (const f of files) {
  const text = fs.readFileSync(f, 'utf8');
  const lines = text.replace(/\r\n/g, '\n').split('\n').length;
  largest = Math.max(largest, lines);
  const rel = path.relative(root, f).split(path.sep).join('/');
  if (lines > max) errors.push(`${rel}: ${lines} > maxLines ${max}`);
  else if (lines > warn) overWarn.push({ rel, lines });
}

if (overWarn.length) {
  console.warn(`WARN ${overWarn.length} file(s) over warnLines ${warn}`);
  for (const x of overWarn.slice(0, 20)) console.warn(`  ${x.rel}: ${x.lines}`);
}

const ratchetPath = path.join(__dirname, 'architecture-ratchet-state.json');
fs.writeFileSync(
  ratchetPath,
  JSON.stringify(
    {
      filesOverWarn: overWarn.length,
      largestFileLines: largest,
      generatedAt: new Date().toISOString(),
    },
    null,
    2
  ) + '\n'
);

if (errors.length) {
  for (const e of errors) console.error('ERROR', e);
  process.exit(1);
}
console.log(
  `check:architecture OK (${files.length} files, largest ${largest}, overWarn ${overWarn.length})`
);
