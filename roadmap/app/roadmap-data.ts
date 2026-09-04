export const priorities = ['Now', 'Next', 'Later', 'Decision'] as const;
export const workTypes = ['Fix', 'Feature'] as const;

export type Priority = (typeof priorities)[number];
export type WorkType = (typeof workTypes)[number];

export type RoadmapItem = {
  id: string;
  title: string;
  description: string;
  area: string;
  priority: Priority;
  workType: WorkType;
  completed: boolean;
  instruction: string;
  custom?: boolean;
};

type SeedItem = Omit<RoadmapItem, 'completed' | 'instruction'>;

const seed = (id: string, title: string, description: string, area: string, priority: Priority): SeedItem =>
  ({
    id,
    title,
    description,
    area,
    priority,
    workType: id.startsWith('F') || id.startsWith('P') ? 'Fix' : 'Feature',
  });

export const roadmapSeed: RoadmapItem[] = [
  seed('F1', 'Fix Release branding', 'Replace the Release-only logo loading path that throws UnauthorizedAccessException and add a published-build branding smoke test.', 'Confirmed fixes', 'Now'),
  seed('F2', 'Fix detached-preview shutdown', 'Remove the NullReferenceException when the main app closes while LIVE Preview is detached, retaining the native regression test.', 'Confirmed fixes', 'Now'),
  seed('F3', 'Repair the compact Braid header', 'Stop the Braid heading and explanation collapsing into an unusably narrow column.', 'Confirmed fixes', 'Now'),
  seed('F4', 'Create shared responsive module headers', 'Use the same responsive header component for Braid, Buncher, Dual and future modules.', 'Confirmed fixes', 'Now'),
  seed('F5', 'Repair Production Speeds compact text', 'Keep the explanatory copy readable at narrow desktop and laptop widths.', 'Confirmed fixes', 'Next'),
  seed('F6', 'Add missing accessibility names', 'Name Home construction tiles, the navigation-pane button and unnamed COR/preview controls for UI Automation and screen readers.', 'Confirmed fixes', 'Now'),
  seed('F7', 'Modernise Dual Insulation presentation', 'Bring Dual onto the layout and usability standard established by COR and the shared module pages.', 'Confirmed fixes', 'Next'),
  seed('P1', 'Profile the complete application', 'Measure startup, route changes, bindings, recalculation, retained-data projection, XAML layout and preview allocations.', 'Performance', 'Now'),
  seed('P2', 'Reduce the memory high-water mark', 'Improve on the current ~387.6 MB working set measured after visiting every page.', 'Performance', 'Now'),
  seed('P3', 'Reduce UI-thread work', 'Find unnecessary bindings, projections, layout passes and geometry recreation that make the interface lag.', 'Performance', 'Now'),
  seed('P4', 'Build one shared preview renderer', 'Use one immutable cable-scene/result model and reusable renderer instead of separate large preview implementations.', 'Performance', 'Next'),
  seed('P5', 'Keep rendering event-driven', 'Render only when data, camera or size changes and remain completely suspended while hidden.', 'Performance', 'Next'),
  seed('P6', 'Cache visual resources', 'Reuse meshes, paths, brushes and materials while avoiding large per-strand XAML visual trees.', 'Performance', 'Next'),
  seed('P7', 'Preserve preview fallbacks', 'Keep Preview Off, lightweight Simple mode and tested WARP software rendering available.', 'Performance', 'Next'),
  seed('P8', 'Test on a representative older laptop', 'Run usability and performance acceptance on integrated-graphics Windows 11 hardware.', 'Performance', 'Next'),
  seed('P9', 'Expand renderer lifecycle testing', 'Cover device loss, monitor removal, DPI changes, repeated resize/pop-out/redock and multi-monitor placement.', 'Performance', 'Later'),
  seed('M1', 'Slim MainPage into an application shell', 'Move page implementations out so MainPage primarily owns navigation and session orchestration.', 'Modularity', 'Now'),
  seed('M2', 'Define the module contract', 'Standardise navigation metadata, header, editor, results, trace, optional preview, save and report capabilities.', 'Modularity', 'Now'),
  seed('M3', 'Give each module clear ownership', 'Each module should own its View, ViewModel and Application service.', 'Modularity', 'Next'),
  seed('M4', 'Split oversized source files', 'Break up MainPage XAML/code, central-data interface code and the large COR/Dual ViewModels.', 'Modularity', 'Now'),
  seed('M5', 'Create a shared easy-build layout', 'Common layout changes should automatically reach COR, Dual, Flat and D-shape costing pages.', 'Modularity', 'Now'),
  seed('M6', 'Keep unique construction modules independent', 'Construction-specific layers should plug into the shared workflow without duplicating the shell.', 'Modularity', 'Next'),
  seed('M7', 'Migrate Dual without calculation changes', 'Preserve Dual calculations, revisions, saves and conductor geometry while moving it onto the shared shell.', 'Modularity', 'Next'),
  seed('M8', 'Generalise preview state', 'Replace the COR-specific scene/session boundary with an app-wide cable-scene/v1-style contract.', 'Modularity', 'Next'),
  seed('M9', 'Define module tool-window behaviour', 'Support linked and standalone tools, explicit Apply to costing, safe placement and one calculation/rendering state.', 'Modularity', 'Later'),
  seed('B1', 'Redesign the Braid lay-up selector', 'Present number of cores, lay-up and OD factor as a structured, readable multi-column selection.', 'Braid & Buncher', 'Next'),
  seed('B2', 'Add approved reverse Braid calculation', 'Calculate suitable machine settings from desired coverage and geometry after the rule is approved.', 'Braid & Buncher', 'Later'),
  seed('B3', 'Insert Braid results into saved costings', 'Allow compatible cable constructions to consume the authoritative Braid module result.', 'Braid & Buncher', 'Later'),
  seed('B4', 'Approve Braid acceptance examples', 'Approve forward, reverse and infeasible-geometry golden cases.', 'Braid & Buncher', 'Decision'),
  seed('B5', 'Render Braid as a construction layer', 'Make Braid part of the shared physical cable scene when the app-wide renderer is ready.', 'Braid & Buncher', 'Later'),
  seed('D1', 'Move Dual onto the shared layout', 'Use the common module structure and responsive behaviour.', 'Dual Insulation', 'Next'),
  seed('D2', 'Add the Dual LIVE Preview', 'Render both insulation layers through the shared scene and renderer.', 'Dual Insulation', 'Next'),
  seed('D3', 'Add Dual-specific document wording', 'Use correct quotation and contract-review wording for Dual constructions.', 'Dual Insulation', 'Later'),
  seed('D4', 'Approve the corrected Dual golden fixture', 'Approve the complete corrected workbook case and source map.', 'Dual Insulation', 'Decision'),
  seed('C1', 'Integrate coiling into dynamic costing', 'Add coiling machine time, labour and commercial cost only inside the future dynamic costing workflow.', 'Coil', 'Later'),
  seed('C2', 'Decide whether Coil needs a preview', 'Add a coil visual only if it communicates useful, approved engineering information.', 'Coil', 'Decision'),
  seed('C3', 'Resolve the workbook +5/-5 tolerance', 'Do not implement the unexplained tolerance until its physical meaning is approved.', 'Coil', 'Decision'),
  seed('S1', 'Persist Production Speed evidence', 'Store the exact speed-library evidence snapshot inside an approved costing revision.', 'Production Speeds', 'Later'),
  seed('S2', 'Validate the estimator with real run data', 'Test recommendations against the larger machine and known-run dataset when supplied.', 'Production Speeds', 'Next'),
  seed('N1', 'Implement Flat cable costing', 'Build the 1–10 in-line-core calculation, geometry, saving and reporting workflow.', 'Future modules', 'Later'),
  seed('N2', 'Implement D-shape cable costing', 'Build its calculation, geometry, saving and reporting workflow.', 'Future modules', 'Later'),
  seed('N3', 'Build the dynamic construction editor', 'Support ordered Tape, Chalk, Foil, Screen, Drain, Braid and Lapscreen modules.', 'Future modules', 'Later'),
  seed('N4', 'Complete the Reports module', 'Replace the placeholder with selectable printable outputs.', 'Reporting', 'Later'),
  seed('N5', 'Add an editable quotation preview', 'Provide an accurate page preview rather than only generating the final PDF.', 'Reporting', 'Later'),
  seed('N6', 'Approve final document templates', 'Finalise quotation, costing-summary and contract-review branding and wording.', 'Reporting', 'Decision'),
  seed('N7', 'Add technical datasheets', 'Generate only approved technical claims from the same saved result model.', 'Reporting', 'Later'),
  seed('N8', 'Add a printable calculation appendix', 'Expose formula, input, unit and rounding evidence in reports.', 'Reporting', 'Later'),
  seed('N9', 'Add revision and scenario tools', 'Provide revision comparison, history, search, scenario comparison and archive.', 'Project workflow', 'Later'),
  seed('N10', 'Define electronic approval', 'Add electronic approval only after user permissions and authority are defined.', 'Project workflow', 'Decision'),
  seed('O1', 'Accept real database connections', 'Test authorised Access and SQL sources; the latest audit had 0 of 5 links configured.', 'Data & operations', 'Next'),
  seed('O2', 'Run a connected one-hour soak test', 'Verify the new hourly automatic refresh policy against approved live sources.', 'Data & operations', 'Next'),
  seed('O3', 'Approve central-data authority', 'Define authoritative tables, keys, mappings, units, effective dates and price selection.', 'Data & operations', 'Decision'),
  seed('O4', 'Finalise authentication and approval ownership', 'Agree secret handling and responsibility before any two-way database workflow.', 'Data & operations', 'Decision'),
  seed('O5', 'Add backup, restore and migration diagnostics', 'Protect retained data and projects and provide understandable import/migration reporting.', 'Data & operations', 'Later'),
  seed('O6', 'Add organisation code signing', 'Remove Unknown Publisher and reduce SmartScreen friction for released installers.', 'Data & operations', 'Later'),
  seed('O7', 'Repeat full installed-app regression', 'Retest updates, save-folder selection, PDF generation and physical printing after architecture changes.', 'Data & operations', 'Next'),
  seed('O8', 'Review and publish the current local fixes', 'Commit and release the NuGet/hourly-refresh work only after review and the chosen defect boundary.', 'Data & operations', 'Next'),
  seed('Q1', 'Approve the first masterbatch golden fixture', 'Confirm the source workbook, mapped cells, allowance meaning and expected cached outputs.', 'Business decisions', 'Decision'),
  seed('Q2', 'Confirm the global display-rounding rule', 'Decide whether midpoint-away-from-zero applies globally or whether some families differ.', 'Business decisions', 'Decision'),
  seed('Q3', 'Define source ownership', 'Name the authority for Copper, Compounds, Masterbatch, Contacts and Operators data.', 'Business decisions', 'Decision'),
  seed('Q4', 'Map remaining workbook calculation families', 'Document and approve each remaining calculation before migration.', 'Business decisions', 'Decision'),
].map((item) => ({ ...item, completed: false, instruction: '' }));
