import { defineConfig, Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'node:path';

function inlinePortableBundle(): Plugin {
  return {
    name: 'inline-portable-bundle',
    enforce: 'post',
    generateBundle(_options, bundle) {
      const htmlEntry = Object.values(bundle).find((entry) => entry.type === 'asset' && entry.fileName.endsWith('.html'));
      if (!htmlEntry || htmlEntry.type !== 'asset') return;

      let html = String(htmlEntry.source);
      for (const [fileName, entry] of Object.entries(bundle)) {
        if (entry.type === 'chunk' && entry.isEntry) {
          html = html.replace(
            new RegExp(`<script[^>]+src=["'][^"']*${fileName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}["'][^>]*></script>`),
            () => `<script type="module">${entry.code.replace(/<\/script/gi, '<\\/script')}</script>`,
          );
          delete bundle[fileName];
        } else if (entry.type === 'asset' && fileName.endsWith('.css')) {
          html = html.replace(
            new RegExp(`<link[^>]+href=["'][^"']*${fileName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}["'][^>]*>`),
            () => `<style>${String(entry.source).replace(/<\/style/gi, '<\\/style')}</style>`,
          );
          delete bundle[fileName];
        }
      }
      htmlEntry.source = html;
    },
  };
}

export default defineConfig({
  root: resolve(__dirname, 'portable'),
  base: './',
  plugins: [react(), inlinePortableBundle()],
  build: {
    outDir: resolve(__dirname, 'portable-dist'),
    emptyOutDir: true,
    assetsInlineLimit: Number.MAX_SAFE_INTEGER,
    cssCodeSplit: false,
  },
});
