<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>Las columnas Miller del Finder de macOS, reimaginadas para Windows.</strong><br>
  Para usuarios avanzados que cambiaron a Windows pero nunca dejaron de extrañar la vista en columnas.
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="Última versión"></a>
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="Licencia"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="Patrocinar"></a>
</p>

<p align="center">
  <a href="../README.md">English</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.ja.md">日本語</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.zh-TW.md">繁體中文</a> |
  <a href="README.de.md">Deutsch</a> |
  <strong>Español</strong> |
  <a href="README.fr.md">Français</a> |
  <a href="README.pt.md">Português</a>
</p>

---

![Navegación con columnas Miller](miller-columns.gif)

> **Navegue jerarquías de carpetas como debían navegarse.**
> Haga clic en una carpeta y su contenido aparece en la siguiente columna. Siempre ve dónde está, de dónde viene y hacia dónde va — todo a la vez. Se acabó hacer clic atrás y adelante.

![LumiFinder — columnas Miller en acción](hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  Si LumiFinder le resulta útil, considere darle una ⭐ — ¡ayuda a otros a descubrir este proyecto!
</p>

---

## ¿Por qué LumiFinder?

| | Explorador de Windows | LumiFinder |
|---|---|---|
| **Columnas Miller** | No | Sí — navegación jerárquica multi-columna |
| **Multi-pestaña** | Solo Windows 11 (básico) | Pestañas completas con desacople, re-acople, duplicación, restauración de sesión |
| **Vista dividida** | No | Doble panel con modos de vista independientes |
| **Panel de vista previa** | Básico | 10+ tipos de archivo — imágenes, video, audio, código, hex, fuentes, PDF |
| **Navegación por teclado** | Limitada | Más de 30 atajos, búsqueda type-ahead, diseño teclado-primero |
| **Renombrar por lotes** | No | Regex, prefijo/sufijo, numeración secuencial |
| **Deshacer/Rehacer** | Limitado | Historial completo de operaciones (profundidad configurable) |
| **Color de acento personalizado** | No | 10 colores predefinidos + tema claro/oscuro/sistema |
| **Densidad de diseño** | No | 6 niveles de altura de fila + escala fuente/icono independiente |
| **Conexiones remotas** | No | FTP, FTPS, SFTP con credenciales guardadas |
| **Espacios de trabajo** | No | Guarde y restaure layouts de pestañas nombradas al instante |
| **Repisa de archivos** | No | Área de tránsito drag & drop estilo Yoink |
| **Estado de la nube** | Superposición básica | Insignias de sincronización en tiempo real (OneDrive, iCloud, Dropbox) |
| **Velocidad de inicio** | Lento en directorios grandes | Carga asíncrona con cancelación — sin retraso |

---

## Características

### Columnas Miller — Vea todo a la vez

Navegue jerarquías de carpetas profundas sin perder el contexto. Cada columna representa un nivel — haga clic en una carpeta y su contenido aparece en la siguiente columna. Siempre ve dónde está y de dónde viene.

- Separadores de columna arrastrables para anchos personalizados
- Auto-igualar columnas (Ctrl+Mayús+=) o auto-ajustar al contenido (Ctrl+Mayús+-)
- Desplazamiento horizontal suave que mantiene visible la columna activa
- Diseño estable — sin temblor de scroll en navegación con teclado ↑/↓

### Cuatro modos de vista

- **Columnas Miller** (Ctrl+1) — Navegación jerárquica, la firma de LumiFinder
- **Detalles** (Ctrl+2) — Tabla ordenable con nombre, fecha, tipo, tamaño
- **Lista** (Ctrl+3) — Layout multi-columna denso para escanear directorios grandes
- **Iconos** (Ctrl+4) — Vista de cuadrícula con 4 tamaños hasta 256×256

### Multi-pestaña con restauración completa de sesión

- Pestañas ilimitadas, cada una con su propia ruta, modo de vista e historial
- **Desacople y re-acople de pestañas**: Arrastre una pestaña fuera para crear una nueva ventana, arrástrela de vuelta para acoplarla — indicador fantasma estilo Chrome y feedback de ventana semi-transparente
- **Duplicación de pestañas**: Clone una pestaña con su ruta exacta y configuraciones
- Auto-guardado de sesión: Cierre la app, vuelva a abrirla — todas las pestañas exactamente donde las dejó

### Vista dividida — Verdadero panel dual

![Vista dividida con columnas Miller + vista previa de código](2.png)

- Navegación lado a lado con navegación independiente por panel
- Cada panel puede usar un modo de vista diferente (Miller izquierda, Detalles derecha)
- Paneles de vista previa separados para cada panel
- Arrastre archivos entre paneles para copiar/mover

### Panel de vista previa — Sepa antes de abrir

Pulse **Espacio** para Quick Look (estilo Finder de macOS):

- **Navegación con flechas y Espacio**: Examine archivos sin cerrar Quick Look
- **Persistencia del tamaño de ventana**: Quick Look recuerda su último tamaño
- **Imágenes**: JPEG, PNG, GIF, BMP, WebP, TIFF con resolución y metadatos
- **Video**: MP4, MKV, AVI, MOV, WEBM con controles de reproducción
- **Audio**: MP3, AAC, M4A con artista, álbum, duración
- **Texto y código**: Más de 30 extensiones con visualización de sintaxis
- **PDF**: Vista previa de la primera página
- **Fuentes**: Muestras de glifos con metadatos
- **Binario hexadecimal**: Vista de bytes en bruto para desarrolladores
- **Carpetas**: Tamaño, número de elementos, fecha de creación
- **Hash de archivo**: Suma SHA256 con copia en un clic (activable en Configuración)

### Diseño teclado-primero

Más de 30 atajos de teclado para usuarios que mantienen las manos en el teclado:

| Atajo | Acción |
|----------|--------|
| Flechas | Navegar columnas y elementos |
| Enter | Abrir carpeta o ejecutar archivo |
| Espacio | Alternar panel de vista previa |
| Ctrl+L / Alt+D | Editar barra de direcciones |
| Ctrl+F | Buscar |
| Ctrl+C / X / V | Copiar / Cortar / Pegar |
| Ctrl+Z / Y | Deshacer / Rehacer |
| Ctrl+Mayús+N | Nueva carpeta |
| F2 | Renombrar (renombrado por lotes con multi-selección) |
| Ctrl+T / W | Nueva pestaña / Cerrar pestaña |
| Ctrl+Tab / Ctrl+Mayús+Tab | Ciclar pestañas |
| Ctrl+1-4 | Cambiar modo de vista |
| Ctrl+Mayús+E | Alternar vista dividida |
| F6 | Cambiar panel de vista dividida |
| Ctrl+Mayús+S | Guardar espacio de trabajo |
| Ctrl+Mayús+W | Abrir paleta de espacios de trabajo |
| Ctrl+Mayús+H | Alternar extensiones de archivo |
| Mayús+F10 | Menú contextual nativo completo del shell |
| Supr | Mover a la papelera |

### Temas y personalización

![Configuración — Apariencia con acento personalizado + densidad de diseño](4.png)

- Seguimiento de tema **Claro / Oscuro / Sistema**
- **10 colores de acento predefinidos** — sobreescriba el acento de cualquier tema con un clic (Lumi Gold por defecto)
- **6 niveles de densidad de diseño** — XS / S / M / L / XL / XXL alturas de fila
- **Escala fuente/icono independiente** — separada de la densidad de fila
- **9 idiomas**: Inglés, coreano, japonés, chino (simplificado/tradicional), alemán, español, francés, portugués (BR)

### Configuración general

![Configuración — General con vista dividida + opciones de vista previa](3.png)

- **Comportamiento de inicio por panel** — Abrir unidad del sistema / Restaurar última sesión / Ruta personalizada, izquierda y derecha independientemente
- **Modo de vista de inicio** — elija Columnas Miller / Detalles / Lista / Iconos por panel
- **Panel de vista previa** — activar al inicio o alternar bajo demanda con Espacio
- **Repisa de archivos** — repisa de tránsito estilo Yoink opcional, con persistencia opcional entre sesiones
- **Bandeja del sistema** — minimizar a la bandeja en lugar de cerrar

### Herramientas para desarrolladores

- **Insignias de estado Git**: Modified, Added, Deleted, Untracked por archivo
- **Visor hex dump**: Primeros 512 bytes en hex + ASCII
- **Integración de terminal**: Ctrl+` abre terminal en la ruta actual
- **Conexiones remotas**: FTP/FTPS/SFTP con almacenamiento cifrado de credenciales

### Integración con almacenamiento en la nube

- **Insignias de estado de sincronización**: Solo nube, Sincronizado, Subida pendiente, Sincronizando
- **OneDrive, iCloud, Dropbox** detectados automáticamente
- **Vistas previas inteligentes**: Usa vistas previas en caché — nunca activa descargas no deseadas

### Búsqueda inteligente

- **Consultas estructuradas**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **Type-ahead**: Comience a escribir en cualquier columna para filtrar instantáneamente
- **Procesamiento en segundo plano**: La búsqueda nunca congela la UI

### Espacio de trabajo — Guardar y restaurar layouts de pestañas

- **Guardar pestañas actuales**: Clic derecho en cualquier pestaña → "Guardar layout de pestañas..." o pulse Ctrl+Mayús+S
- **Restaurar al instante**: Haga clic en el botón de espacio de trabajo en la barra lateral o pulse Ctrl+Mayús+W
- **Gestionar espacios de trabajo**: Restaurar, renombrar o eliminar layouts guardados desde el menú de espacios de trabajo
- Perfecto para cambiar entre contextos de trabajo — "Desarrollo", "Edición de fotos", "Documentos"

### Repisa de archivos

- Área de tránsito drag & drop estilo Yoink de macOS
- Arrastre archivos a la repisa mientras navega, suéltelos donde los necesite
- Eliminar un elemento de la repisa solo elimina la referencia — el archivo original nunca se toca
- Desactivada por defecto — actívela en **Configuración > General > Recordar elementos de la repisa**

---

## Rendimiento

Diseñado para velocidad. Probado con más de 10.000 elementos por carpeta.

- I/O asíncrono en todas partes — nada bloquea el hilo de UI
- Actualizaciones de propiedades por lotes con sobrecarga mínima
- Selección con debounce previene trabajo redundante durante navegación rápida
- Caché por pestaña — cambio instantáneo de pestaña, sin re-renderizado
- Carga concurrente de vistas previas con throttling SemaphoreSlim

---

## Requisitos del sistema

| | |
|---|---|
| **OS** | Windows 10 versión 1903+ / Windows 11 |
| **Arquitectura** | x64, ARM64 |
| **Runtime** | Windows App SDK 1.8 (.NET 8) |
| **Recomendado** | Windows 11 para fondo Mica |

---

## Compilar desde el código fuente

```bash
# Requisitos previos: Visual Studio 2022 con cargas de trabajo .NET Desktop + WinUI 3

# Clonar
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# Compilar
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# Ejecutar pruebas unitarias
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **Nota**: Las apps WinUI 3 no se pueden ejecutar mediante `dotnet run`. Use **Visual Studio F5** (requiere empaquetado MSIX).

---

## Contribuir

¿Encontró un bug? ¿Tiene una solicitud de característica? [Abra una issue](https://github.com/LumiBearStudio/LumiFinder/issues) — todo feedback es bienvenido.

Vea [CONTRIBUTING.md](../CONTRIBUTING.md) para configuración de compilación, convenciones de código y guías de PR.

---

## Apoyar el proyecto

Si LumiFinder mejora su gestión de archivos, considere:

- **[Patrocinar en GitHub](https://github.com/sponsors/LumiBearStudio)** — invíteme un café, una hamburguesa o un bistec
- **Dele una estrella ⭐** a este repositorio para ayudar a otros a descubrirlo
- **Comparta** con colegas que extrañan el Finder de macOS en Windows
- **Reporte bugs** — cada issue hace a LumiFinder más estable

---

## Privacidad y telemetría

LumiFinder usa [Sentry](https://sentry.io) **solo para reportes de fallos** — y puede desactivarlo.

- **Lo que recolectamos**: Tipo de excepción, stack trace, versión del SO, versión de la app
- **Lo que NO recolectamos**: Nombres de archivos, rutas de carpetas, historial de navegación, información personal
- **Sin analítica de uso, sin tracking, sin anuncios**
- Todas las rutas de archivo en reportes de fallos se limpian automáticamente antes de enviarse
- `SendDefaultPii = false` — sin direcciones IP ni identificadores de usuario
- **Opt-out**: Configuración > Avanzado > Interruptor "Reportes de fallos" para desactivar completamente
- Código abierto — verifíquelo usted mismo en [`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

Vea la [Política de privacidad](../PRIVACY.md) para detalles completos.

---

## Licencia

Este proyecto está licenciado bajo la [GNU General Public License v3.0](../LICENSE).

**Marca**: El nombre "LumiFinder" y el logo oficial son marcas comerciales de LumiBear Studio. Los forks deben usar un nombre y logo diferentes. Vea [LICENSE.md](../LICENSE.md) para la política completa de marcas.

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">Patrocinar</a> ·
  <a href="../PRIVACY.md">Política de privacidad</a> ·
  <a href="../OpenSourceLicenses.md">Licencias de código abierto</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">Reportes de bugs y solicitudes de características</a>
</p>
