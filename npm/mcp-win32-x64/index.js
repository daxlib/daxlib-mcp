'use strict';

// Runs the self-contained daxlib-mcp executable carried in dist/, passing the caller's stdio
// straight through: stdout stays the MCP JSON-RPC stream, untouched by this wrapper.

const childProcess = require('child_process');
const path = require('path');

const packageJson = require('./package.json');

function runExecutable(args) {
  const executableRelativePath = Object.values(packageJson.bin)[0];
  const executablePath = path.join(__dirname, executableRelativePath);

  return new Promise((resolve, reject) => {
    const child = childProcess.spawn(executablePath, args, {
      stdio: 'inherit',
      shell: false,
    });

    child.on('error', reject);
    child.on('close', (code) => resolve(code === null ? 1 : code));
  });
}

module.exports = { runExecutable };

if (require.main === module) {
  runExecutable(process.argv.slice(2))
    .then((code) => process.exit(code))
    .catch((error) => {
      console.error(error);
      process.exit(1);
    });
}
