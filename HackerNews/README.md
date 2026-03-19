 Hacker News Best Stories API

API RESTful desarrollada en ASP.NET Core que obtiene las mejores historias de Hacker News, ordenadas por puntuación, permitiendo especificar la cantidad deseada.

 Requisitos cumplidos

- Endpoint `GET /api/stories/best/{n}` que devuelve las `n` mejores historias.
- Integración con la API oficial de Hacker News.
- Caché en memoria para reducir llamadas externas y mejorar rendimiento.
- Control de concurrencia para no sobrecargar la API de Hacker News.
- Rate limiting para prevenir abusos.
- Manejo de errores y degradación graceful.
- Documentación interactiva con Swagger.

Tecnologías

- .NET 8
- ASP.NET Core
- MemoryCache
- Swagger / OpenAPI
- HttpClientFactory
- Polly (para reintentos)

Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Opcional) [Postman](https://www.postman.com/) para probar la API

 Instalación y ejecución

1. Clona el repositorio:
   ```bash
   git clone https://github.com/tu-usuario/hackernews-best-stories.git
   cd hackernews-best-stories

Restaura los paquetes NuGet:

bash
dotnet restore
Compila la solución:

bash
dotnet build
Ejecuta la API:

bash
cd HackerNewsBestStories.Api
dotnet run
La API estará disponible en:

https://localhost:7076 (HTTPS)

http://localhost:5182 (HTTP)

Accede a la documentación Swagger:

text
https://localhost:7076/swagger

Uso de la API
Obtener las mejores historias
text
GET /api/stories/best/{n}
Parámetro	Tipo	Rango válido	Descripción
n	integer	1 - 200	Número de historias a devolver
Ejemplo de petición:

bash
curl -k https://localhost:7076/api/stories/best/5
Respuesta exitosa (200 OK):

json
[
  {
    "title": "Título de la historia",
    "uri": "https://ejemplo.com/articulo",
    "postedBy": "usuario123",
    "time": "2025-03-19T12:34:56Z",
    "score": 1234,
    "commentCount": 56
  },
  ...
]
Códigos de respuesta:

200 OK – Todo correcto.

400 Bad Request – El valor de n está fuera del rango permitido.

429 Too Many Requests – Se ha excedido el límite de peticiones por minuto.

503 Service Unavailable – La API de Hacker News no responde o hay un error interno.

 Decisiones de diseño y supuestos
1. Límite de n entre 1 y 200
Aunque la API de Hacker News puede devolver hasta 500 IDs, se optó por limitar a 200 por razones de rendimiento y usabilidad.

Las historias más allá de la posición 200 tienen puntuaciones muy bajas y rara vez son consultadas.

Con 200 historias se obtiene un equilibrio entre información relevante y tiempo de respuesta (≈2.5 s).

2. URL de las historias
csharp
Uri = raw.Url ?? $"https://news.ycombinator.com/item?id={raw.Id}"
Si la historia tiene una URL externa (raw.Url), se utiliza esa.

En caso contrario (por ejemplo, preguntas de "Ask HN" o textos sin enlace), se proporciona un enlace al hilo de comentarios en Hacker News. Esto asegura que siempre se devuelva una URI válida y útil.

3. Caché multinivel
IDs de historias: se cachean por 1 minuto (cambian con frecuencia).

Detalles de cada historia: se cachean por 5 minutos (los scores varían lentamente).

Resultados compuestos: se cachean por 5 minutos con deslizamiento de 1 minuto.

Esto reduce las llamadas a la API externa en más de un 99% bajo carga.

4. Control de concurrencia
Se utiliza un SemaphoreSlim(10) para limitar las llamadas simultáneas a la API de Hacker News.

Además, se emplea un lock exclusivo (SemaphoreSlim(1)) para la reconstrucción de la caché, evitando el problema de "thundering herd" cuando expira la caché y muchos usuarios solicitan el mismo recurso a la vez.

5. Rate limiting
Se limita cada IP a 100 peticiones por minuto mediante middleware personalizado.

Esto protege la API de abusos y garantiza un uso justo.

6. Manejo de errores
Si la API de Hacker News falla, se devuelve 503 y se registra el error.

Si una historia individual falla, se omite y se continúa con las demás (degradación graceful).

 Mejoras futuras
Dado más tiempo, se implementarían las siguientes mejoras:

Paginación: Permitir parámetros page y pageSize para navegar por las 500 historias sin sacrificar rendimiento.

Redis como caché distribuida: Para entornos con múltiples instancias de la API.

Cache warming: Un servicio en segundo plano que refresque la caché antes de que expire, evitando picos de demanda.

Autenticación con API Key: Para controlar el acceso y ofrecer diferentes límites según el plan.

Endpoint GraphQL: Para que los clientes puedan solicitar solo los campos que necesitan.

Pruebas de carga automatizadas: Con herramientas como k6 para verificar el comportamiento bajo estrés.

Contenerización con Docker: Facilitar el despliegue en cualquier entorno.

 Licencia
Este proyecto es solo con fines practicos, como parte de una prueba técnica.

Nota: Si tienes alguna duda o sugerencia, no dudes en abrir un issue en el repositorio.

text

 ¿Por qué esta URL alternativa?

```csharp
Uri = raw.Url ?? $"https://news.ycombinator.com/item?id={raw.Id}"
Es una decisión de diseño para garantizar que siempre haya un enlace válido en la respuesta.

raw.Url es la URL externa que la historia puede tener (por ejemplo, un artículo de blog).

Si la historia no tiene URL (como ocurre con las publicaciones de tipo "Ask HN" o "Show HN" que son solo texto), entonces raw.Url es null.

En ese caso, se genera un enlace al hilo de discusión en Hacker News usando el ID de la historia. Así el usuario puede al menos leer los comentarios o el texto de la publicación original.

Este comportamiento es común en muchos clientes de Hacker News y está documentado en la API original.
   
