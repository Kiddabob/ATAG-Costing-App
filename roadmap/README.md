# ATAG Costing development roadmap

Open `Open ATAG Development Roadmap.cmd` in the parent `ATAG Design Ltd`
folder. The launcher resolves its location relative to itself, so it keeps
working when the USB drive letter changes.

The generated `portable-dist` page is local and does not need Node.js, a web
server, or an internet connection. The page filters by priority, Fix/Feature,
completion and section, and saves its order, checkboxes, priorities, work
types, custom tasks, and task instructions in the current browser's local
storage. Use **Download backup** before moving to another PC,
then use **Import backup** there. **Download for Codex** creates an ordered
Markdown plan that can be attached to a future development chat.

Development commands:

- `pnpm run build` validates the Vite/React source application.
- `pnpm run build:portable` refreshes the self-contained page opened by the
  root launcher.
