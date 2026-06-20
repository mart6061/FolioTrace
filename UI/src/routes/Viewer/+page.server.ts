import { _loadAssetPageData } from '../Asset/+page.server';
import { _loadReportPageData } from '../Report/+page.server';
import type { PageServerLoad } from './$types';

const viewerKeys = ['Asset', 'Report', 'Metric'] as const;

type ViewerKey = typeof viewerKeys[number];

export const load: PageServerLoad = async (event) => {
  const viewer = normalizeViewer(event.url.searchParams.get('viewer'));

  if (viewer === 'Report') {
    return {
      asset: null,
      report: await _loadReportPageData(event),
      viewer
    };
  }

  if (viewer === 'Metric') {
    return {
      asset: null,
      report: null,
      viewer
    };
  }

  return {
    asset: await _loadAssetPageData(event),
    report: null,
    viewer
  };
};

function normalizeViewer(value: string | null): ViewerKey {
  return viewerKeys.includes(value as ViewerKey) ? value as ViewerKey : 'Asset';
}
