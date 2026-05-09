# LumiFinder — Política de privacidad

**Última actualización: 9 de mayo de 2026**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <strong>Español</strong> |
  <a href="PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## Resumen

LumiFinder ("la App") es una aplicación de explorador de archivos para Windows desarrollada por LumiBear Studio. Estamos comprometidos con la protección de su privacidad. Esta política explica qué datos recopilamos, cómo los protegemos y cómo puede controlarlos.

## Datos que recopilamos

### Reportes de fallos (Sentry)

La App utiliza [Sentry](https://sentry.io) para reportes automáticos de fallos. Cuando la App falla o encuentra un error no manejado, los siguientes datos **pueden** ser enviados:

- **Detalles del error**: Tipo de excepción, mensaje y stack trace
- **Información del dispositivo**: Versión del SO, arquitectura de CPU, uso de memoria al momento del fallo
- **Información de la app**: Versión de la app, versión del runtime, configuración de compilación

Los reportes de fallos se utilizan **únicamente** para identificar y corregir errores. **No** incluyen:

- Nombres de archivos, nombres de carpetas o contenido de archivos
- Información de cuenta de usuario
- Historial de navegación o rutas de navegación
- Cualquier información personalmente identificable (PII)

### Protecciones de privacidad en los reportes de fallos

Antes de enviar cualquier reporte de fallo, se aplican automáticamente múltiples capas de limpieza de PII:

- **Enmascaramiento de nombre de usuario** — Las rutas de carpeta de usuario de Windows (`C:\Users\<su-nombre-de-usuario>\...`) se detectan y la parte del nombre de usuario se reemplaza antes de la transmisión. Lo mismo se aplica a las rutas UNC (`\\servidor\compartido\Users\<nombre-de-usuario>\...`).
- **`SendDefaultPii = false`** — La recolección automática del SDK de Sentry de direcciones IP, nombres de servidores e identificadores de usuario está completamente desactivada.
- **Sin contenido de archivos** — Los stack traces nunca contienen contenido de archivos o carpetas; solo números de línea y nombres de métodos.

Puede verificar la implementación usted mismo en el código abierto:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### Configuración local

La App almacena las preferencias del usuario (tema, idioma, carpetas recientes, favoritos, color de acento personalizado, etc.) localmente en su dispositivo usando `ApplicationData.LocalSettings` de Windows. Estos datos **nunca** se transmiten a ningún servidor.

## Datos que NO recopilamos

- Sin información personal (nombre, correo electrónico, dirección)
- Sin contenidos del sistema de archivos o metadatos de archivos
- Sin análisis de uso o telemetría
- Sin datos de ubicación
- Sin identificadores publicitarios
- Sin datos compartidos con terceros para marketing

## Acceso a la red

La App requiere acceso a internet solo para:

- **Reportes de fallos** (Sentry) — reportes automáticos de errores, se pueden desactivar (ver "Cómo optar por no participar" abajo)
- **Conexiones FTP / FTPS / SFTP** — solo cuando son explícitamente configuradas por el usuario
- **Restauración de paquetes NuGet** — solo durante compilaciones de desarrollo (no se ejecuta para usuarios finales)

## Cómo optar por no recibir reportes de fallos

Los reportes de fallos se pueden desactivar directamente desde la App sin desconectarse de internet:

1. Abra **Configuración** (parte inferior izquierda de la barra lateral)
2. Navegue a **Avanzado**
3. Desactive **Reportes de fallos**

El cambio surte efecto inmediatamente. Después de optar por no participar, no se enviarán reportes de fallos bajo ninguna circunstancia. Los reportes pasados ya en los servidores de Sentry seguirán expirando según el calendario estándar de retención de 90 días.

## Almacenamiento y retención de datos

- **Servidores de Sentry**: Los reportes de fallos se almacenan en el centro de datos de **Frankfurt, Alemania (UE)** de Sentry — elegido para el cumplimiento del RGPD. Los reportes se eliminan automáticamente después de **90 días**.
- **Configuración local**: Almacenada solo en su dispositivo. Se elimina al desinstalar la App.

## Sentry como Encargado del tratamiento (RGPD)

Sentry actúa como Encargado del tratamiento (Data Processor) para los reportes de fallos bajo el RGPD. Para detalles sobre las prácticas de privacidad y medidas de seguridad de Sentry:

- **Política de privacidad de Sentry**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Seguridad de Sentry**: [https://sentry.io/security/](https://sentry.io/security/)
- **RGPD de Sentry**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio ha revisado los términos de procesamiento de datos de Sentry y seleccionó la región de la UE (`o4510949994266624.ingest.de.sentry.io`) para asegurar que los datos de fallos no salgan del Espacio Económico Europeo sin las garantías apropiadas.

## Privacidad de los niños

La App no recopila a sabiendas datos de niños menores de 13 años. La App no está dirigida a niños y no recopila ninguna información personal que pueda identificar a un niño.

## Sus derechos

Dado que no recopilamos datos personales, no hay datos personales para acceder, modificar o eliminar. Específicamente:

- **Derecho de acceso / portabilidad**: No aplicable — no tenemos datos personales suyos.
- **Derecho de eliminación**: No aplicable — no tenemos datos personales suyos. La configuración local se puede eliminar desinstalando la App.
- **Derecho a optar por no participar**: Disponible en Configuración > Avanzado > Reportes de fallos (ver "Cómo optar por no participar" arriba).

## Código abierto

LumiFinder es de código abierto bajo la licencia GPL v3.0. Es bienvenido a inspeccionar, auditar o modificar el código usted mismo:

- **Código fuente**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **Bibliotecas de código abierto utilizadas**: Vea [OpenSourceLicenses.md](../OpenSourceLicenses.md)

## Contacto

Si tiene preguntas sobre esta política de privacidad, encontró una violación o desea ejercer los derechos descritos arriba:

- **Issues de GitHub**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **Divulgación de seguridad**: Vea [SECURITY.md](../SECURITY.md)

## Cambios a esta política

Podemos actualizar esta política de vez en cuando a medida que la App evolucione o cambien los requisitos legales. Los cambios materiales se anunciarán a través de GitHub Releases. Cada actualización aumenta la fecha de "Última actualización" en la parte superior de este documento. El historial de versiones está disponible permanentemente en el [historial de Git](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md).
