async function search() {
  const query = document.getElementById('query').value;
  const response = await fetch(/api/search?query=);
  const data = await response.json();
  document.getElementById('results').innerText = JSON.stringify(data, null, 2);
}
