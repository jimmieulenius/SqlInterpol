#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { findRepoRoot } = require('../lib/repo-root');

const root = findRepoRoot(__dirname);
const cfg = JSON.parse(
  fs.readFileSync(path.join(__dirname, 'documentation-ownership.json'), 'utf8')
);

const errors = [];

function nonEmptyLines(section) {
  return section
    .split('\n')
    .map((l) => l.trim())
    .filter((l) => l.length > 0).length;
}

function hasFastPath(text) {
  return (
    /^## Fast path\b/m.test(text) ||
    /^## Read order\b/m.test(text) ||
    /^## Read path\b/m.test(text)
  );
}

function fastPathBlock(text) {
  const m = text.match(
    /^## (?:Fast path|Read order|Read path)[^\n]*\n([\s\S]*?)(?=^## |\Z)/m
  );
  return m ? m[1] : '';
}

for (const rel of cfg.decidingHomes || []) {
  const abs = path.join(root, rel);
  if (!fs.existsSync(abs)) {
    errors.push(`missing deciding home: ${rel}`);
    continue;
  }
  const text = fs.readFileSync(abs, 'utf8');
  if (!hasFastPath(text)) {
    errors.push(`${rel}: missing ## Fast path (or Read order / Read path)`);
    continue;
  }
  const n = nonEmptyLines(fastPathBlock(text));
  const max = cfg.fastPathMaxNonEmptyLines || 25;
  if (n > max) {
    errors.push(`${rel}: Fast path has ${n} non-empty lines > ${max}`);
  }
}

const canonicalRel = cfg.canonicalTaskTable || 'docs/CANONICAL-SOURCES.md';
const canonicalAbs = path.join(root, canonicalRel);
if (fs.existsSync(canonicalAbs)) {
  const text = fs.readFileSync(canonicalAbs, 'utf8');
  const taskSection = text.split(/## Task/i)[1] || '';
  const openCells = [...taskSection.matchAll(/\|\s*([^|\n]+)\s*\|\s*([^|\n]+)\s*\|/g)];
  const owners = new Map();
  for (const row of openCells) {
    const task = row[1].trim();
    const open = row[2].trim();
    if (/^Task$/i.test(task) || /^---/.test(task) || /^Open/i.test(open)) continue;
    const mdLinks = [...open.matchAll(/\(([^)]+\.md)(?:#[^)]*)?\)/g)].map((x) => x[1]);
    if (mdLinks.length > 1) {
      errors.push(
        `${canonicalRel}: task "${task}" Open cell has multiple .md links: ${mdLinks.join(', ')}`
      );
    }
    for (const link of mdLinks) {
      if (!owners.has(link)) owners.set(link, []);
      owners.get(link).push(task);
    }
  }
}

if (errors.length) {
  for (const e of errors) console.error('ERROR', e);
  process.exit(1);
}
console.log('check:documentation:ownership OK');
