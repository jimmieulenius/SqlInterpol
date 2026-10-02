#!/usr/bin/env node
'use strict';

/**
 * Rank 7 unused-export / Knip-class gate.
 * Modes (unused-export.json next to this script):
 * - queued: dated Rank 7 deferral (owner + due required); exits 0
 * - knip: run `npx knip` (or config.command) from repo root
 * Never silent forever-skip: missing owner/due in queued mode fails.
 */

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');
const { findRepoRoot } = require('../lib/repo-root');

const dir = __dirname;
const root = findRepoRoot(dir);
const cfgPath = path.join(dir, 'unused-export.json');

if (!fs.existsSync(cfgPath)) {
  console.error(
    'ERROR missing unused-export.json — copy from kit or set mode queued|knip (Rank 7; see docs/ops/code-quality-and-refactor.md)'
  );
  process.exit(1);
}

const cfg = JSON.parse(fs.readFileSync(cfgPath, 'utf8'));
const mode = (cfg.mode || '').toLowerCase();

function requireClock(label) {
  const owner = (cfg.owner || '').trim();
  const due = (cfg.due || '').trim();
  const errors = [];
  if (!owner) errors.push(`${label}: missing owner`);
  if (!due) errors.push(`${label}: missing due (YYYY-MM-DD)`);
  else if (!/^\d{4}-\d{2}-\d{2}$/.test(due)) errors.push(`${label}: due must be YYYY-MM-DD (got ${due})`);
  if (errors.length) {
    for (const e of errors) console.error('ERROR', e);
    console.error('Rank 7 clocked queue requires owner + due — not soft skip.');
    process.exit(1);
  }
}

if (mode === 'queued') {
  requireClock('unused-export queued');
  console.log(
    `check:unused-export OK (Rank 7 queued; owner=${cfg.owner}; due=${cfg.due}; tool=${cfg.tool || 'knip-class'})`
  );
  process.exit(0);
}

if (mode === 'knip') {
  const cmd = cfg.command || 'npx';
  const args = cfg.args || ['knip'];
  const run = spawnSync(cmd, args, { cwd: root, encoding: 'utf8', shell: true });
  process.stdout.write(run.stdout || '');
  process.stderr.write(run.stderr || '');
  if (run.status !== 0) {
    console.error(
      'check:unused-export failed. Fix findings, or set mode to queued with owner + due (Rank 7).'
    );
    process.exit(run.status || 1);
  }
  console.log('check:unused-export OK (knip)');
  process.exit(0);
}

console.error(
  `ERROR unused-export.json mode "${cfg.mode}" unknown — use "queued" or "knip"`
);
process.exit(1);
