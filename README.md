 🛰️ Orbit Hub

> *Tu biblioteca. Tus juegos. Tus reglas.*

**Orbit Hub** es un lanzador de juegos minimalista, rápido y ligero para Windows, construido desde cero con **C#** y **Avalonia UI**. Diseñado para ser una alternativa moderna y de bajo consumo de recursos a otros launchers, con un enfoque en la personalización y el rendimiento.


 ✨ Características

- 🚀 **Ligero y rápido**: Construido sobre .NET 8 y Avalonia, sin la sobrecarga de un navegador (como Electron).
- 📂 **Escaneo automático**: Detecta tus juegos simplemente seleccionando la carpeta principal de tu biblioteca.
- 🖼️ **Carátulas en Alta Resolución**: Extrae los iconos de los archivos `.exe` en calidad 256x256 (Jumbo) usando la API nativa de Windows.
- 📝 **Carátulas Personalizadas**: ¿El juego tiene un icono genérico? Clic derecho y elige tu propia imagen `.png` o `.jpg`.
- 🔍 **Buscador Integrado**: Encuentra cualquier juego de tu biblioteca al instante mientras escribes.
- ⭐ **Sistema de Favoritos**: Marca tus juegos favoritos con una estrella. La app los filtra en una pestaña dedicada y los recuerda para siempre.
- 🎮 **Formato Póster**: Interfaz con tarjetas verticales, al estilo de los launchers más modernos.
- 🖱️ **Lanzamiento rápido**: Doble clic en cualquier juego para iniciarlo.
- 💾 **Persistencia**: Recuerda tu biblioteca, favoritos y personalizaciones entre sesiones usando un archivo JSON local.

 🛠️ Tecnologías utilizadas

- **Framework**: [.NET 8](https://dotnet.microsoft.com/)
- **UI**: [Avalonia UI](https://avaloniaui.net/) (v12)
- **Arquitectura**: MVVM con [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- **Extracción de iconos**: `IShellItemImageFactory` (API nativa de Windows Shell32)

 🚀 Cómo compilar y ejecutar

Si quieres probar el código o contribuir, sigue estos pasos:

1. Asegúrate de tener instalado el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clona el repositorio:
   ```bash
   git clone https://github.com/TU-USUARIO/OrbitHub.git