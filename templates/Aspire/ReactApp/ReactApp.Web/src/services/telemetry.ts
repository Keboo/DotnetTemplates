import { ApplicationInsights } from '@microsoft/applicationinsights-web'

let appInsights: ApplicationInsights | undefined

export function initTelemetry(): void {
  const connectionString = __APPLICATIONINSIGHTS_CONNECTION_STRING__
  if (!connectionString) {
    return
  }

  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true,
      enableCorsCorrelation: true,
      enableRequestHeaderTracking: true,
      enableResponseHeaderTracking: true,
    },
  })

  appInsights.loadAppInsights()
}

export function getAppInsights(): ApplicationInsights | undefined {
  return appInsights
}
