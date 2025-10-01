# Elevator Simulation API

A backend API for simulating elevator operations using C# and .NET 8.

## Configuration
<ol>
  <li>Update appsettings.json with the desired values</li>
  <li>Configure CORS to allow your frontend (FE) to connect. Example:</li>
</ol>

```
"CorsSettings": {
  "AllowedOrigins": "http://localhost:5173"
}
```

> Replace http://localhost:5173 with your frontend URL if different.

## Installation & Running
<ol>
  <li>Open the solution in Visual Studio 2022 (recommended).</li>
  <li>Select IIS Express as the launch profile.</li>
  <li>Click Run (or press F5) to start the API.</li>
</ol>

### Screenshot

<img width="457" height="333" alt="image" src="https://github.com/user-attachments/assets/03e2cdec-890f-4daa-ad6c-19324c9d0ba6" />

## Testing (xUnit)
<ol>
  <li>A separate <code>Elevator.Simulation.Tests</code> project is included.</li>
  <li>It uses <a href="https://xunit.net/">xUnit</a> and <code>Microsoft.Extensions.Logging.Abstractions</code> for testing.</li>
</ol>

## Notes

<ul>
  <li>Ensure your frontend is allowed in the CORS settings.</li>
</ul>

---
