#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');
const { findRepoRoot } = require('../lib/repo-root');

const dir = __dirname;
const root = findRepoRoot(dir);
const ratchet = JSON.parse(
  fs.readFileSync(path.join(dir, 'architecture-ratchet.json'), 'utf8')
);

const run = spawnSync(process.execPath, [path.join(dir, 'check-architecture.js')], {
  cwd: root,
  encoding: 'utf8',
});
if (run.status !== 0) {
  process.stdout.write(run.stdout || '');
  process.stderr.write(run.stderr || '');
  process.exit(run.status || 1);
}

const statePath = path.join(dir, 'architecture-ratchet-state.json');
if (!fs.existsSync(statePath)) {
  console.error('ERROR missing architecture-ratchet-state.json (run check:architecture first)');
  process.exit(1);
}
const state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
const errors = [];

if (state.filesOverWarn > ratchet.maxFilesOverWarn) {
  errors.push(
    `filesOverWarn ${state.filesOverWarn} > ceiling ${ratchet.maxFilesOverWarn} (tighten or extract; do not raise casually)`
  );
}
if (state.largestFileLines > ratchet.maxLargestFileLines) {
  errors.push(
    `largestFileLines ${state.largestFileLines} > ceiling ${ratchet.maxLargestFileLines}`
  );
}

if (errors.length) {
  for (const e of errors) console.error('ERROR', e);
  console.error(ratchet.policy || 'Ceilings only tighten.');
  process.exit(1);
}
console.log('check:architecture:ratchet OK');
