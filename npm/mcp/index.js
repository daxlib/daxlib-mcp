#!/usr/bin/env node
'use strict';

// Thin universal launcher: resolves the platform-specific package that carries the actual
// daxlib-mcp executable and hands over stdio untouched. No downloads, no postinstall scripts —
// the binary travels inside the platform package, installed by npm's own os/cpu gating.

const os = require('os');

const packageName = '@daxlib/mcp';
const platform = os.platform();
const arch = os.arch();
const platformPackageName = `${packageName}-${platform}-${arch}`;

let platformPackage;

try {
  platformPackage = require(platformPackageName);
} catch (error) {
  console.error(`${packageName}: the platform package '${platformPackageName}' is not installed.`);
  console.error('');
  console.error(`Supported platforms: win32-x64. Current platform: ${platform}-${arch}.`);
  console.error('');
  console.error('If you are on a supported platform, try:');
  console.error(`  npm install ${platformPackageName}`);
  console.error('');
  console.error('For help, see https://github.com/daxlib/daxlib-mcp.');
  process.exit(1);
}

platformPackage
  .runExecutable(process.argv.slice(2))
  .then((code) => process.exit(code))
  .catch((error) => {
    console.error(`${packageName}: failed to run the server executable.`);
    console.error(error);
    process.exit(1);
  });
