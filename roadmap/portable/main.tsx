import React from 'react';
import { createRoot } from 'react-dom/client';
import RoadmapApp from '../app/roadmap-app';
import '../app/globals.css';

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <RoadmapApp />
  </React.StrictMode>,
);
