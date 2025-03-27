async function search() {
  const query = document.getElementById('query').value;
  // Use the correct path, matching the Route attribute in your Function
  const apiUrl = `http://localhost:7071/api/search?query=${encodeURIComponent(query)}`;

  try {
    const response = await fetch(apiUrl);

    if (!response.ok) {
      // Handle non-200 responses (e.g., 400, 500)
      console.error(`HTTP error! status: ${response.status}`);
      document.getElementById('results').innerText = `Error: ${response.statusText}`; // Display error message on UI
      return; // Exit the function if there's an error
    }

    const data = await response.json(); // Parse JSON response
    document.getElementById('results').innerText = JSON.stringify(data, null, 2); // Pretty-print JSON
  } catch (error) {
    console.error("Fetch error:", error);
    document.getElementById('results').innerText = `Error: ${error.message}`; // Display error message on UI
  }
}