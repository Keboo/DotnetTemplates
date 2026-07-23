import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { VitePWA } from 'vite-plugin-pwa';

// Get backend URL from Aspire service discovery or fallback.
// Aspire sets environment variables in the format: services__{service-name}__{protocol}__{index}.
const backendUrl = process.env.BACKEND_URL
  || process.env.APP_BACKEND_HTTP
  || process.env.APP_BACKEND_HTTPS
  || process.env['services__AspireApp-backend__http__0']
  || process.env['services__AspireApp-backend__https__0']
  || process.env.REACTAPP_BACKEND_HTTP
  || process.env.REACTAPP_BACKEND_HTTPS
  || process.env.services__backend__http__0
  || process.env.services__backend__https__0
  || 'https://localhost:5001';

export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.ico', 'robots.txt', 'apple-touch-icon.png'],
      manifest: {
        name: 'AspireApp',
        short_name: 'AspireApp',
        description: 'A Progressive Web App built with Vue and Vite',
        theme_color: '#ffffff',
        background_color: '#ffffff',
        display: 'standalone',
        start_url: '/',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable'
          }
        ]
      },
      devOptions: {
        enabled: true
      }
    })
  ],
  define: {
    __API_BASE_URL__: JSON.stringify(backendUrl),
    __APPLICATIONINSIGHTS_CONNECTION_STRING__: JSON.stringify(process.env.APPLICATIONINSIGHTS_CONNECTION_STRING || '')
  },
  server: {
    host: process.env.VITE_HOST_URL || 'localhost',
    port: parseInt(process.env.PORT || '5173', 10),
    proxy: {
      '/api': {
        target: backendUrl,
        changeOrigin: true,
        secure: false,
        cookieDomainRewrite: 'localhost'
      },
      '/hubs': {
        target: backendUrl,
        changeOrigin: true,
        secure: false,
        ws: true,
        cookieDomainRewrite: 'localhost'
      }
    }
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    sourcemap: true
  }
});
