'use client';

import { ChangeEvent, DragEvent, useEffect, useMemo, useRef, useState } from 'react';
import { priorities, Priority, roadmapSeed, RoadmapItem, workTypes, WorkType } from './roadmap-data';

const STORAGE_KEY = 'atag-costing-roadmap-v1';
const THEME_KEY = 'atag-costing-roadmap-theme';
type Theme = 'system' | 'light' | 'dark';
type ViewFilter = 'all' | 'open' | 'done';
type WorkTypeFilter = 'all' | WorkType;

const cloneSeed = () => roadmapSeed.map((item) => ({ ...item }));

function downloadFile(name: string, type: string, content: string) {
  const blob = new Blob([content], { type });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  link.click();
  URL.revokeObjectURL(url);
}

function normaliseItems(value: unknown): RoadmapItem[] | null {
  if (!Array.isArray(value)) return null;
  const validPriorities = new Set<string>(priorities);
  const validWorkTypes = new Set<string>(workTypes);
  const items = value.filter((item): item is Record<string, unknown> => Boolean(item) && typeof item === 'object');
  if (!items.length) return null;
  return items.map((item, index) => ({
    id: typeof item.id === 'string' ? item.id : `imported-${index}-${Date.now()}`,
    title: typeof item.title === 'string' ? item.title : 'Untitled task',
    description: typeof item.description === 'string' ? item.description : '',
    area: typeof item.area === 'string' ? item.area : 'Custom',
    priority: validPriorities.has(String(item.priority)) ? (item.priority as Priority) : 'Later',
    workType: validWorkTypes.has(String(item.workType))
      ? (item.workType as WorkType)
      : String(item.id).startsWith('F') || String(item.id).startsWith('P') ? 'Fix' : 'Feature',
    completed: Boolean(item.completed),
    instruction: typeof item.instruction === 'string' ? item.instruction : '',
    custom: Boolean(item.custom),
  }));
}

export default function RoadmapApp() {
  const [items, setItems] = useState<RoadmapItem[]>(cloneSeed);
  const [hydrated, setHydrated] = useState(false);
  const [view, setView] = useState<ViewFilter>('all');
  const [workTypeFilter, setWorkTypeFilter] = useState<WorkTypeFilter>('all');
  const [priorityFilter, setPriorityFilter] = useState<Priority[]>([...priorities]);
  const [area, setArea] = useState('All areas');
  const [search, setSearch] = useState('');
  const [theme, setTheme] = useState<Theme>('system');
  const [status, setStatus] = useState('Loading your saved plan…');
  const [draggedId, setDraggedId] = useState<string | null>(null);
  const [showAdd, setShowAdd] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newDescription, setNewDescription] = useState('');
  const [newArea, setNewArea] = useState('Custom');
  const [newWorkType, setNewWorkType] = useState<WorkType>('Feature');
  const importInput = useRef<HTMLInputElement>(null);

  useEffect(() => {
    try {
      const storedTheme = localStorage.getItem(THEME_KEY) as Theme | null;
      if (storedTheme && ['system', 'light', 'dark'].includes(storedTheme)) setTheme(storedTheme);
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored) as { items?: unknown } | unknown[];
        const restored = normaliseItems(Array.isArray(parsed) ? parsed : parsed.items);
        if (restored) setItems(restored);
      }
      setStatus(stored ? 'Saved plan restored' : 'Ready — your plan saves on this PC');
    } catch {
      setStatus('Browser storage is unavailable — use JSON backup');
    } finally {
      setHydrated(true);
    }
  }, []);

  useEffect(() => {
    const root = document.documentElement;
    root.dataset.theme = theme;
    try { localStorage.setItem(THEME_KEY, theme); } catch { /* backup remains available */ }
  }, [theme]);

  useEffect(() => {
    if (!hydrated) return;
    setStatus('Saving changes…');
    const timer = window.setTimeout(() => {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify({ version: 2, savedAt: new Date().toISOString(), items }));
        setStatus(`Saved automatically at ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`);
      } catch {
        setStatus('Could not save locally — download a JSON backup');
      }
    }, 450);
    return () => window.clearTimeout(timer);
  }, [items, hydrated]);

  const areas = useMemo(() => ['All areas', ...Array.from(new Set(items.map((item) => item.area))).sort()], [items]);
  const visibleItems = useMemo(() => {
    const query = search.trim().toLowerCase();
    return items.filter((item) => {
      if (view === 'open' && item.completed) return false;
      if (view === 'done' && !item.completed) return false;
      if (workTypeFilter !== 'all' && item.workType !== workTypeFilter) return false;
      if (!priorityFilter.includes(item.priority)) return false;
      if (area !== 'All areas' && item.area !== area) return false;
      return !query || `${item.id} ${item.title} ${item.description} ${item.instruction} ${item.area}`.toLowerCase().includes(query);
    });
  }, [items, view, workTypeFilter, priorityFilter, area, search]);

  const completed = items.filter((item) => item.completed).length;
  const progress = items.length ? Math.round((completed / items.length) * 100) : 0;
  const fixCount = items.filter((item) => item.workType === 'Fix').length;
  const featureCount = items.length - fixCount;

  const togglePriority = (priority: Priority) => {
    setPriorityFilter((current) => current.includes(priority)
      ? current.filter((candidate) => candidate !== priority)
      : priorities.filter((candidate) => candidate === priority || current.includes(candidate)));
  };

  const updateItem = (id: string, patch: Partial<RoadmapItem>) => {
    setItems((current) => current.map((item) => item.id === id ? { ...item, ...patch } : item));
  };

  const moveItem = (id: string, delta: number) => {
    setItems((current) => {
      const from = current.findIndex((item) => item.id === id);
      const to = Math.max(0, Math.min(current.length - 1, from + delta));
      if (from < 0 || from === to) return current;
      const next = [...current];
      const [moved] = next.splice(from, 1);
      next.splice(to, 0, moved);
      return next;
    });
  };

  const dropBefore = (targetId: string) => {
    if (!draggedId || draggedId === targetId) return;
    setItems((current) => {
      const next = [...current];
      const from = next.findIndex((item) => item.id === draggedId);
      const moved = next[from];
      if (!moved) return current;
      next.splice(from, 1);
      const target = next.findIndex((item) => item.id === targetId);
      next.splice(target < 0 ? next.length : target, 0, moved);
      return next;
    });
    setDraggedId(null);
  };

  const saveNow = () => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ version: 2, savedAt: new Date().toISOString(), items }));
      setStatus(`Saved at ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`);
    } catch {
      setStatus('Could not save locally — download a JSON backup');
    }
  };

  const exportJson = () => downloadFile(
    `ATAG-Costing-roadmap-${new Date().toISOString().slice(0, 10)}.json`,
    'application/json',
    JSON.stringify({ version: 2, exportedAt: new Date().toISOString(), items }, null, 2),
  );

  const exportMarkdown = () => {
    const lines = [
      '# ATAG Costing ordered development plan',
      '',
      `Exported: ${new Date().toLocaleString()}`,
      `Progress: ${completed} of ${items.length} complete (${progress}%)`,
      '',
      ...items.flatMap((item, index) => [
        `## ${index + 1}. [${item.completed ? 'x' : ' '}] ${item.id} — ${item.title}`,
        '',
        `- Area: ${item.area}`,
        `- Type: ${item.workType}`,
        `- Priority: ${item.priority}`,
        `- Scope: ${item.description}`,
        ...(item.instruction ? [`- My instruction: ${item.instruction}`] : []),
        '',
      ]),
    ];
    downloadFile(`ATAG-Costing-ordered-plan-${new Date().toISOString().slice(0, 10)}.md`, 'text/markdown', lines.join('\n'));
  };

  const importJson = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    try {
      const parsed = JSON.parse(await file.text()) as { items?: unknown } | unknown[];
      const restored = normaliseItems(Array.isArray(parsed) ? parsed : parsed.items);
      if (!restored) throw new Error('No roadmap items found');
      setItems(restored);
      setStatus(`Imported ${restored.length} tasks from ${file.name}`);
    } catch {
      setStatus('That file is not a valid ATAG roadmap backup');
    }
  };

  const addItem = () => {
    const title = newTitle.trim();
    if (!title) return;
    setItems((current) => [...current, {
      id: `U${current.filter((item) => item.custom).length + 1}`,
      title,
      description: newDescription.trim(),
      area: newArea.trim() || 'Custom',
      priority: 'Later',
      workType: newWorkType,
      completed: false,
      instruction: '',
      custom: true,
    }]);
    setNewTitle('');
    setNewDescription('');
    setNewWorkType('Feature');
    setShowAdd(false);
  };

  const openAdd = (targetArea?: string) => {
    setNewArea(targetArea && targetArea !== 'All areas' ? targetArea : 'Custom');
    setShowAdd(true);
  };

  const resetPlan = () => {
    if (window.confirm('Reset the list, order, checkboxes and instructions to the original plan? Download a backup first if you may want them later.')) {
      setItems(cloneSeed());
      setView('all');
      setWorkTypeFilter('all');
      setPriorityFilter([...priorities]);
      setArea('All areas');
      setSearch('');
      setStatus('Original plan restored');
    }
  };

  return (
    <main className="app-shell">
      <header className="topbar">
        <a className="brand" href="#top" aria-label="ATAG Costing roadmap home">
          <span className="brand-mark">A</span>
          <span><strong>ATAG Costing</strong><small>Development roadmap</small></span>
        </a>
        <div className="top-actions">
          <span className="save-status" role="status">{status}</span>
          <select className="compact-select" value={theme} onChange={(event) => setTheme(event.target.value as Theme)} aria-label="Colour theme">
            <option value="system">System theme</option>
            <option value="light">Light theme</option>
            <option value="dark">Dark theme</option>
          </select>
          <button className="button button-primary" onClick={saveNow}>Save now</button>
        </div>
      </header>

      <section className="hero" id="top">
        <div className="hero-copy">
          <span className="eyebrow">Plan clearly · build confidently</span>
          <h1>Shape the next version.</h1>
          <p>Tick off completed work, drag tasks into your preferred order, and add your own instructions before handing the plan back to Codex.</p>
          <div className="hero-actions">
            <button className="button button-primary button-large" onClick={() => openAdd()}>＋ Add a task</button>
            <button className="button button-tonal button-large" onClick={exportMarkdown}>Download for Codex</button>
          </div>
        </div>
        <div className="progress-card">
          <div className="progress-ring" style={{ '--progress': `${progress * 3.6}deg` } as React.CSSProperties}>
            <span><strong>{progress}%</strong><small>complete</small></span>
          </div>
          <div><strong>{completed} finished</strong><span>{items.length - completed} still open</span><span>{fixCount} fixes · {featureCount} features</span></div>
        </div>
        <div className="shape shape-one" aria-hidden="true" />
        <div className="shape shape-two" aria-hidden="true" />
      </section>

      <section className="workspace" aria-label="Roadmap planner">
        <aside className="control-panel">
          <div>
            <span className="eyebrow">View</span>
            <div className="segmented" role="group" aria-label="Completion filter">
              {(['all', 'open', 'done'] as ViewFilter[]).map((option) => (
                <button key={option} className={view === option ? 'active' : ''} onClick={() => setView(option)}>{option[0].toUpperCase() + option.slice(1)}</button>
              ))}
            </div>
          </div>
          <div>
            <span className="eyebrow">Work type</span>
            <div className="segmented work-type-segmented" role="group" aria-label="Fix or feature filter">
              {(['all', 'Fix', 'Feature'] as WorkTypeFilter[]).map((option) => (
                <button key={option} className={workTypeFilter === option ? 'active' : ''} onClick={() => setWorkTypeFilter(option)}>
                  {option === 'all' ? 'All' : `${option}s`}
                </button>
              ))}
            </div>
          </div>
          <div className="priority-filter-block">
            <div className="filter-label-row"><span className="eyebrow">Priority</span><button className="text-button" onClick={() => setPriorityFilter([...priorities])}>Select all</button></div>
            <div className="priority-pills" role="group" aria-label="Priority filters">
              {priorities.map((priority) => (
                <button
                  key={priority}
                  className={`priority-pill priority-pill-${priority.toLowerCase()} ${priorityFilter.includes(priority) ? 'active' : ''}`}
                  aria-pressed={priorityFilter.includes(priority)}
                  onClick={() => togglePriority(priority)}
                >
                  <i aria-hidden="true" />{priority}
                </button>
              ))}
            </div>
          </div>
          <label className="field-label">Find a task<input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search titles or notes…" /></label>
          <label className="field-label">Section<select value={area} onChange={(event) => setArea(event.target.value)}>{areas.map((option) => <option key={option}>{option}</option>)}</select></label>
          <button className="button button-section" onClick={() => openAdd(area)}>＋ Add to {area === 'All areas' ? 'a section' : area}</button>
          <div className="helper-card"><strong>Arrange your plan</strong><p>Drag the dotted handle, or use the arrow buttons. Your order and ticks save automatically in this browser.</p></div>
          <div className="panel-actions">
            <button className="button button-tonal" onClick={exportJson}>Download backup</button>
            <button className="button button-quiet" onClick={() => importInput.current?.click()}>Import backup</button>
            <button className="button button-quiet danger" onClick={resetPlan}>Reset original</button>
            <input ref={importInput} type="file" accept="application/json,.json" hidden onChange={importJson} />
          </div>
        </aside>

        <div className="task-region">
          <div className="list-heading">
            <div><span className="eyebrow">Your order</span><h2>{visibleItems.length} task{visibleItems.length === 1 ? '' : 's'} shown</h2></div>
            <span className="legend"><i /> Now <i /> Next <i /> Later <i /> Decision</span>
          </div>

          {showAdd && (
            <section className="add-card" aria-label="Add a task">
              <div><span className="eyebrow">New task</span><h3>Add something we missed</h3></div>
              <label>Title<input autoFocus value={newTitle} onChange={(event) => setNewTitle(event.target.value)} placeholder="What needs doing?" /></label>
              <label>Description<textarea value={newDescription} onChange={(event) => setNewDescription(event.target.value)} placeholder="Add enough context for a future development chat." /></label>
              <label>Section<input value={newArea} onChange={(event) => setNewArea(event.target.value)} /></label>
              <label>Work type<select value={newWorkType} onChange={(event) => setNewWorkType(event.target.value as WorkType)}>{workTypes.map((option) => <option key={option}>{option}</option>)}</select></label>
              <div className="add-actions"><button className="button button-quiet" onClick={() => setShowAdd(false)}>Cancel</button><button className="button button-primary" onClick={addItem} disabled={!newTitle.trim()}>Add to end</button></div>
            </section>
          )}

          <div className="task-list">
            {visibleItems.map((item, visibleIndex) => {
              const globalIndex = items.findIndex((candidate) => candidate.id === item.id);
              return (
                <article
                  key={item.id}
                  className={`task-card priority-${item.priority.toLowerCase()} ${item.completed ? 'completed' : ''} ${draggedId === item.id ? 'dragging' : ''}`}
                  onDragOver={(event: DragEvent) => event.preventDefault()}
                  onDrop={() => dropBefore(item.id)}
                >
                  <button
                    className="drag-handle"
                    draggable
                    onDragStart={(event) => { setDraggedId(item.id); event.dataTransfer.effectAllowed = 'move'; }}
                    onDragEnd={() => setDraggedId(null)}
                    title="Drag to reorder"
                    aria-label={`Drag ${item.title} to reorder`}
                  >⠿</button>
                  <label className="check-wrap">
                    <input type="checkbox" checked={item.completed} onChange={(event) => updateItem(item.id, { completed: event.target.checked })} />
                    <span aria-hidden="true">✓</span>
                    <span className="sr-only">Mark {item.title} complete</span>
                  </label>
                  <div className="task-content">
                    <div className="task-title-row"><span className="position">{globalIndex + 1}</span><h3>{item.title}</h3><span className="task-id">{item.id}</span></div>
                    <p>{item.description}</p>
                    <div className="task-controls">
                      <span className="area-chip">{item.area}</span>
                      <label className="priority-control"><span>Type</span><select value={item.workType} onChange={(event) => updateItem(item.id, { workType: event.target.value as WorkType })}>{workTypes.map((option) => <option key={option}>{option}</option>)}</select></label>
                      <label className="priority-control"><span>Priority</span><select value={item.priority} onChange={(event) => updateItem(item.id, { priority: event.target.value as Priority })}>{priorities.map((option) => <option key={option}>{option}</option>)}</select></label>
                    </div>
                    <details className="instruction" open={Boolean(item.instruction)}>
                      <summary>{item.instruction ? 'Your instruction added' : 'Add your instruction'}</summary>
                      <textarea value={item.instruction} onChange={(event) => updateItem(item.id, { instruction: event.target.value })} placeholder="Add a decision, acceptance detail, or note for the next development session…" />
                    </details>
                  </div>
                  <div className="move-buttons" aria-label={`Move ${item.title}`}>
                    <button onClick={() => moveItem(item.id, -1)} disabled={globalIndex === 0} title="Move up">↑</button>
                    <button onClick={() => moveItem(item.id, 1)} disabled={globalIndex === items.length - 1} title="Move down">↓</button>
                  </div>
                  <span className="priority-rail" aria-hidden="true" />
                </article>
              );
            })}
            {!visibleItems.length && <div className="empty-state"><span>◎</span><h3>No tasks match this view</h3><p>Try another priority, work type, search or section.</p></div>}
          </div>
        </div>
      </section>

      <footer><strong>ATAG Costing roadmap</strong><span>Local-first · portable · no cloud account required</span></footer>
    </main>
  );
}
