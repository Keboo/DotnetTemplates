import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { createApp } from 'vue';
import App from './App.vue';
import './style.css';
import { register } from './serviceWorkerRegistration';

if (__APPLICATIONINSIGHTS_CONNECTION_STRING__) {
  const appInsights = new ApplicationInsights({
    config: {
      connectionString: __APPLICATIONINSIGHTS_CONNECTION_STRING__,
      enableAutoRouteTracking: true
    }
  });
  appInsights.loadAppInsights();
}

createApp(App).mount('#app');
register();
