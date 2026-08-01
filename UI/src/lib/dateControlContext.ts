import { getContext, setContext } from 'svelte';
import { defaultDateControlConfiguration } from '$lib/dateRules';
import type { DateControlConfiguration } from '$lib/types';

const key = Symbol('date-control-configuration');
type ConfigurationGetter = () => DateControlConfiguration;

export function setDateControlConfiguration(configuration: ConfigurationGetter) { setContext(key, configuration); }
export function getDateControlConfiguration() { return getContext<ConfigurationGetter | undefined>(key) ?? (() => defaultDateControlConfiguration); }
