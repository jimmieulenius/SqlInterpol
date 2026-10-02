#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { findRepoRoot } = require('../lib/repo-root');

const root = findRepoRoot(__dirname);
const budgets = JSON.parse(
  fs.readFileSync(path.join(__dirname, 'documentation-budgets.json'), 'utf8')
);

function walk(dir, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  for (const name of fs.readdirSync(dir)) {
    const p = path.join(dir, name);
    const st = fs.statSync(p);
    if (st.isDirectory()) walk(p, acc);
    else acc.push(p);
  }
  return acc;
}

function matchGlob(fileRel, glob) {
  const norm = fileRel.split(path.sep).join('/');
  if (glob.endsWith('/**/*.md')) {
    const prefix = glob.slice(0, -'/**/*.md'.length);
    return norm.startsWith(prefix + '/') && norm.endsWith('.md');
  }
  if (glob.endsWith('/**')) {
    const prefix = glob.slice(0, -3);
    return norm === prefix || norm.startsWith(prefix + '/');
  }
  return norm === glob;
}

function countLines(text) {
  if (!text) return 0;
  return text.replace(/\r\n/g, '\n').split('\n').length;
}

const errors = [];
const warns = [];

for (const [area, cfg] of Object.entries(budgets.areas || {})) {
  const files = new Set();
  for (const g of cfg.globs || []) {
    if (g.includes('*')) {
      const base = g.split('*')[0].replace(/\/$/, '');
      const abs = path.join(root, base);
      for (const f of walk(abs)) {
        const rel = path.relative(root, f).split(path.sep).join('/');
        if (matchGlob(rel, g)) files.add(rel);
      }
    } else {
      files.add(g.split(path.sep).join('/'));
    }
  }
  for (const rel of files) {
    const abs = path.join(root, rel);
    if (!fs.existsSync(abs)) {
      errors.push(`[${area}] missing file: ${rel}`);
      continue;
    }
    const lines = countLines(fs.readFileSync(abs, 'utf8'));
    if (lines > cfg.max) {
      errors.push(`[${area}] ${rel}: ${lines} lines > max ${cfg.max}`);
    } else if (lines > cfg.warn) {
      warns.push(`[${area}] ${rel}: ${lines} lines > warn ${cfg.warn}`);
    }
  }
}

for (const w of warns) console.warn('WARN', w);
if (errors.length) {
  for (const e of errors) console.error('ERROR', e);
  process.exit(1);
}
console.log('check:documentation OK');
