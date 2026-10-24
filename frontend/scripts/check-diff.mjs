import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "../..");
const gitDirectory = path.join(repositoryRoot, ".git");

const result = spawnSync(
  "git",
  [
    `--git-dir=${gitDirectory}`,
    `--work-tree=${repositoryRoot}`,
    "diff",
    "--check",
  ],
  { cwd: repositoryRoot, stdio: "inherit" },
);

if (result.error) {
  console.error(`Unable to run git diff --check: ${result.error.message}`);
  process.exit(1);
}

process.exit(result.status ?? 1);
