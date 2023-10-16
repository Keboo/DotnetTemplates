# This script leverages the GitHub CLI.
# Ensure you have installed and updated it by following the directions here: https://github.com/cli/cli

# This script assumes it is being executed from the root of a repository 

# Setup acceptable merge types
gh repo edit --enable-merge-commit=false
gh repo edit --enable-squash-merge
gh repo edit --enable-rebase-merge

# Enable PR Auto Merge
 gh repo edit --enable-auto-merge

 #TODO: Setup branch protection rule for default branch