import * as Icons from '@mui/icons-material';

// Função para converter snake_case para PascalCase
const convertToPascalCase = (str) => {
  return str
    .split('_') 
    .map(word => word.charAt(0).toUpperCase() + word.slice(1)) // Capitaliza cada palavra
    .join('');
};

const getMaterialIcon = (iconName) => {
  if (!iconName) {
    console.error(`Icon name is missing or undefined.`);
    return Icons['HelpOutline']; // Ícone de fallback
  }

  const pascalCaseIconName = convertToPascalCase(iconName);
  const IconComponent = Icons[pascalCaseIconName];

  if (!IconComponent) {
    console.error(`Icon "${pascalCaseIconName}" not found.`);
    return Icons['HelpOutline']; // Ícone de fallback
  }

  return IconComponent;
};

export default getMaterialIcon;
