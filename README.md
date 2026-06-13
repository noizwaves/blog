# blog.noizwaves.com

Technical blog of Adam Neumann, built with [Hugo](https://gohugo.io/) and deployed as static HTML.

## Tools

- **Hugo** (extended) — static site generator (`hugo.toml`), version pinned in `.mise.toml`
- **PaperMod** — theme installed as a git submodule under `themes/PaperMod`
- **mise** — manages the Hugo version (`.mise.toml`)

## Setup

```sh
mise install                  # install the pinned Hugo version
git submodule update --init   # fetch the PaperMod theme
```

## Development

```sh
./bin/server.sh   # local dev server at http://localhost:1313
```

## Deployment

```sh
./bin/deploy.sh   # build and rsync to dell-one
```

## Structure

- `content/posts/` — blog posts
- `content/pages/` — standalone pages (About, Resources)
- `static/assets/` — images and other files served verbatim at `/assets/...`
- `archetypes/` — front matter templates for new content
- `themes/PaperMod/` — theme (git submodule)
- `public/` — build output (not committed)

## Writing

Posts live in `content/posts/`. URLs preserve the original scheme via `hugo.toml`
(`/YYYY/MM/DD/<slug>.html`), so each post's `date` and `slug` front matter determine its URL.
Set `draft: true` to keep a post out of the build; preview drafts with `hugo server -D`.
