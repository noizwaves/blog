---
title: Simple Git Branch Squashing
date: 2019-10-17 17:44:00 -0400
---

## The old way

When developing features, I'll often use temporary WIP branches to store WIP commits before squashing them into a single feature commit.
To squash the WIP commits, I have always run `git rebase -i HEAD~x`, where `x` is the number of commits to squash.
To calculate `x`, I run `git log` and manually count each commit.
This week I found a simpler way write that command.

## A simpler way

It turns out that `HEAD~x` is a commit-ish, which refers to the commit `x-1` before `HEAD`.
For trunk based development, `HEAD~1` is actually the head of the trunk branch!
Conveniently, this commit can also be referred to by the trunk branch's name.

Put more concisely:

> When squashing `x` commits atop a branch named `develop`, then `HEAD~x` and `develop` are equivalent commit-ishes.

That means the command `git rebase -i develop` can be used instead of `git rebase -i HEAD~x`.
Same outcome, less cognitive overhead!

## Example

For this blog, the trunk branch is `develop`.
When implementing the Atom Feed feature, the `atom-feed` branch was used to track work in progress.

![Unsquashed WIP branch](/assets/squashing-wip-branches/unsquashed-branch.png)

The `atom-feed` branch contains 4 WIP commits that together comprise the feature.
The goal is to squash these 4 commits into a single commit on develop.

First, to perform the squash I ran `git rebase -i develop` to start an interactive rebase:

![Interactive rebase](/assets/squashing-wip-branches/interactive-rebase.png)

Here I `reword` the first commit (to describe the overall feature) and then `fixup` to squash the final 3 commits into the first.
The resulting commit (`a55bf81`) is our feature commit.
Also, this results in the `atom-feed` branch being 1 commit ahead of `develop`:

![Squashed WIP branch](/assets/squashing-wip-branches/squashed-branch.png)

Finally, after merging the `atom-feed` branch into `develop`, the history looks like this:

![Merged into develop](/assets/squashing-wip-branches/merged-into-develop.png)