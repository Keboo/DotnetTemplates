type Config = {
  onSuccess?: (registration: ServiceWorkerRegistration) => void;
  onUpdate?: (registration: ServiceWorkerRegistration) => void;
};

export function register(config?: Config): void {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  window.addEventListener('load', () => {
    const swUrl = `${import.meta.env.BASE_URL}sw.js`;
    navigator.serviceWorker.register(swUrl).then((registration) => {
      if (registration.active && config?.onSuccess) {
        config.onSuccess(registration);
      }

      registration.onupdatefound = () => {
        if (config?.onUpdate) {
          config.onUpdate(registration);
        }
      };
    }).catch((error) => {
      console.error('Error during service worker registration:', error);
    });
  });
}
