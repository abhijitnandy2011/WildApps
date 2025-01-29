import {
  arrowDownIcon,
  arrowUpIcon,
  trashIcon,
  pencilIcon,
  filePdfIcon,
  plusIcon,
  fileExcelIcon,
  caretAltExpandIcon,
  caretAltToTopIcon,
  caretAltToBottomIcon,
  tableBodyIcon,
  tableUnmergeIcon,
  tableRowGroupsIcon,
  gridIcon,
  SVGIcon,
} from '@progress/kendo-svg-icons';

export interface MenuItems {
  text?: string;
  svgIcon?: SVGIcon;
  children?: childMenuItem[];
  separator?: boolean;
}

export interface childMenuItem {
  childText?: string;
  svgIcon?: SVGIcon;
}

export const menuItems: MenuItems[] = [
  { text: 'Add', svgIcon: plusIcon },
  { text: 'Edit', svgIcon: pencilIcon },
  { text: 'Delete', svgIcon: trashIcon },
  { separator: true },
  {
    text: 'Select',
    svgIcon: tableBodyIcon,
    children: [
      { childText: 'Row', svgIcon: tableRowGroupsIcon },
      { childText: 'All rows', svgIcon: gridIcon },
      { childText: 'Clear selection', svgIcon: tableUnmergeIcon },
    ],
  },
  {
    text: 'Reorder row',
    svgIcon: caretAltExpandIcon,
    children: [
      { childText: 'Up', svgIcon: arrowUpIcon },
      { childText: 'Down', svgIcon: arrowDownIcon },
      { childText: 'Top', svgIcon: caretAltToTopIcon },
      { childText: 'Bottom', svgIcon: caretAltToBottomIcon },
    ],
  },
  { separator: true },
  { text: 'Export to PDF', svgIcon: filePdfIcon },
  { text: 'Export to Excel', svgIcon: fileExcelIcon },
];
