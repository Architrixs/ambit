export default {
  title: 'Ambit',
  description: 'Interactive Region & Annotation Gallery for Avalonia',
  base: '/ambit/', // Subpath for GitHub Pages deployment
  themeConfig: {
    logo: '/logo.svg',
    nav: [
      { text: 'Guide', link: '/guide/getting-started' },
      { text: 'Live Demo', link: '/demo/index.html', target: '_blank' },
      { text: 'GitHub', link: 'https://github.com/archi/ambit' }
    ],
    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'What is Ambit?', link: '/guide/what-is-ambit' },
          { text: 'Getting Started', link: '/guide/getting-started' }
        ]
      },
      {
        text: 'Architecture',
        items: [
          { text: 'Core Logic & Math', link: '/guide/architecture-core' },
          { text: 'Rendering & UI', link: '/guide/architecture-rendering' }
        ]
      },
      {
        text: 'Styling',
        items: [
          { text: 'Labels', link: '/guide/labels' }
        ]
      },
      {
        text: 'Advanced Topics',
        items: [
          { text: 'Events', link: '/guide/events' },
          { text: 'Extensibility (OCP)', link: '/guide/extensibility' },
          { text: 'Serialization & DTOs', link: '/guide/serialization' }
        ]
      }
    ],
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2026'
    }
  }
}
