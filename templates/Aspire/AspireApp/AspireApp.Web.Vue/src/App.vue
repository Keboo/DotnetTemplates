<script setup lang="ts">
const apiBaseUrl = __API_BASE_URL__;
</script>

<template>
  <main class="app">
    <h1>Welcome to your Aspire + Vue app</h1>
    <p>
      This frontend is scaffolded with Vue + Vite and wired to the backend through Aspire service discovery.
    </p>
    <p>
      Backend base URL: <code>{{ apiBaseUrl }}</code>
    </p>
  </main>
</template>
