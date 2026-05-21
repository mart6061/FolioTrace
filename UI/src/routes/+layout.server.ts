import { getSystemVersion } from '$lib/server/api';
import { getUiVersion } from '$lib/server/version';

export const load = async ({ fetch }) => {
  const uiVersion = getUiVersion();

  try {
    const systemVersion = await getSystemVersion(fetch);

    return {
      apiVersion: systemVersion.apiVersion,
      uiVersion
    };
  } catch {
    return {
      apiVersion: 'unavailable',
      uiVersion
    };
  }
};
