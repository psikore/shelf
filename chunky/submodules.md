# Dealing with submodules

## Step 1: Update the submodule(s) to the tag/commit you want

From the main repo root:
```bash
cd path/to/submodule
git fetch --tags
git checkout <tag-or-commit>
```
You can verify:
```bash
git rev-parse HEAD
```

## Step 2: Go back to the main repo and commit the pointer

```bash
cd ../..
git add path/to/submodule
git commit -m "Update submodule to <tag>?\"
```

This is the step people forget.

If you don't commit the pointer, Git will show the submodule as "dirty" or "detached", and your teammates will not get the update.

## Step 3: Push both repos (if needed)

If you made changes inside the submodule itself:

```bash
cd path/to/submodule
git push
```

Then push the main repo:

```bash
cd ../..
git push
```

## How to verify what commit the submodule is actually using

From the main repo:

```bash
git submodule status
```
You will see something like:
```
1a2b3c4d5e6f7g8h9i0 submodules/foo (v1.2.3)
```

That SHA is the *actual* commit the main repo is pointing at.

Inside the submodule:

```bash
cd submodule/foo
git rev-parse HEAD
```

These two SHAs must match.
If they don't match, you're in a detached or dirty state.

# Resetting everything cleanly (if things feel "off")

```bash
git submodule sync --recursive
git submodule update --init --recursive
```

Ths forces Git to:
- re-read the submodule URLs
- check out the exact commit the parent repo expects
- clean up any weirdness

This is the "reset to known good state" button.

# Common pitfalls (and how to avoid them)

X Checking out a branch inside a submodule

Submodules are meant to be pinned to a commit, not a branch.
If you check out a branch, Git will show it as "dirty".

X Forgetting to commit the pointer in the parent repo

This is the #1 cause of confusion.

X Expecting `git pull` to update submodules

It doesn't.
You must run:
```bash
git submodule update --recursive --remote
```
if you want to pull the latest from submodule branches.

X Assuming submodules track tags automatically

They don't.
You must manually check out the tag and commit the pointer.

# To periodically update the submodules to a new tag from the main branch commit.

```bash
# 1. Update the main repo
git checkout my-feature-branch
git pull

# 2. Update submodule to new tag
cd submodules/foo
git fetch --tags
git checkout v2.0.1

# 3. Commit the pointer
cd ../..
git add submodules/foo
git commit -m "Update foo to v2.0.1"

# 4. Push
git push
```
# Rebasing off main

```bash
# 1) update main locally
git fetch origin

# 2) Rebase your branch onto main
git checkout my-app-branch
git rebase origin/main

# 3) Update submodules to match the new pointers
git submodule update --init --recursive

# 4) Verify
git submodule status
```

Or to manually update submodules without re-basing:

```bash
git checkout my-app-branch
git checkout origin/main -- path/to/submodule
git commit -m "Update submodule pointer to match main"
```

# A script

Assumptions
- Submodules live under `submodules/<name>
- tags in the submodule repos follow the same version naming as in the file
- You rnu the script from the root of the main repo

```Python
#!/usr/bin/env python3
import subprocess
import sys
from pathlib import Path


REQ_FILE = "submodule-requirements.txt"  # your requirements-style file


def run(cmd, cwd=None):
    print(f"→ {cmd}")
    result = subprocess.run(cmd, cwd=cwd, shell=True)
    if result.returncode != 0:
        print(f"Command failed: {cmd}")
        sys.exit(result.returncode)


def parse_requirements(path):
    reqs = {}
    for line in Path(path).read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if "==" not in line:
            print(f"Invalid line: {line}")
            sys.exit(1)
        name, version = line.split("==", 1)
        reqs[name.strip()] = version.strip()
    return reqs


def update_submodule(name, version):
    submodule_path = Path(name)
    if not submodule_path.exists():
        print(f"Submodule directory not found: {submodule_path}")
        sys.exit(1)

    print(f"\n=== Updating {name} to tag {version} ===")

    # Fetch tags inside the submodule
    run("git fetch --tags", cwd=submodule_path)

    # Checkout the tag
    run(f"git checkout {version}", cwd=submodule_path)

    # Verify commit SHA
    sha = subprocess.check_output(
        "git rev-parse HEAD", cwd=submodule_path, shell=True
    ).decode().strip()
    print(f"{name} now at commit {sha}")

    # Stage the updated pointer in the parent repo
    run(f"git add {submodule_path}")


def main():
    print("Reading requirements…")
    reqs = parse_requirements(REQ_FILE)

    print("Ensuring submodules are initialized…")
    run("git submodule update --init --recursive")

    for name, version in reqs.items():
        update_submodule(name, version)

    print("\nCommitting updated submodule pointers…")
    run('git commit -m "Update submodules to specified tags"')

    print("\nDone. Push your branch when ready.")


if __name__ == "__main__":
    main()
```

afterwards git push

