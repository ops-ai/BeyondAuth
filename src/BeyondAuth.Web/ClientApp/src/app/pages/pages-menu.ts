import { NbMenuItem } from '@nebular/theme';

export const MENU_ITEMS: NbMenuItem[] = [
  {
    title: 'Overview',
    icon: 'activity-outline',
    link: '/pages/dashboard',
    home: true,
  },
  {
    title: 'AUTHORIZATION',
    group: true,
  },
  {
    title: 'Policies',
    icon: 'shield',
    link: '/pages/policies',
  },
  {
    title: 'Apps & Devices',
    icon: 'cube',
    link: '/pages/apps',
  },
  {
    title: 'Auth Handlers',
    icon: 'code-download',
    link: '/pages/resolvers'
  },
  {
    title: 'AUTHENTICATION',
    group: true,
  },
  {
    title: 'Overview',
    icon: 'lock',
    link: '/pages/directories',
  },
  {
    title: 'Directories',
    icon: 'folder',
    link: '/pages/directories',
  },
  {
    title: 'Clients',
    icon: 'browser',
    link: '/pages/directories',
  },
  {
    title: 'APIs',
    icon: 'grid',
    link: '/pages/directories',
  },
  {
    title: 'Users',
    icon: 'people',
    link: '/pages/users',
  },
  {
    title: 'AUDIT',
    group: true,
  },
  {
    title: 'Reports',
    icon: 'bar-chart',
    link: '/pages/audit-reports',
  },
  {
    title: 'Settings',
    icon: 'settings-2',
    link: '/pages/audit-settings',
  },
  {
    title: 'Organization',
    group: true,
  },
  {
    title: 'Org Settings',
    icon: 'settings',
    link: '/pages/org-settings',
  },
  {
    title: 'Support',
    icon: 'phone-call',
    link: '/pages/support',
  }
];
