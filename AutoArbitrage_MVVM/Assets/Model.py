import requests
import json
import sys

API_KEY = "2myuLonCGqyk94gBt6DLF0D3D7k_2eGJcpAZ3xkNVJB17Pvzi" 
url = "https://tops-hog-hot.ngrok-free.app/news"

def fetch_news():
    headers = {
        "x-api-key": API_KEY
    }

    response = requests.get(url, headers=headers)

    if response.status_code == 200:
        data = response.json()
        return json.dumps(data)  # Convert the data to a JSON string and return it
    else:
        return json.dumps({'error': 'Unable to fetch news'})

if __name__ == "__main__":
    # Execute the fetch_news function and print the result
    news_data = fetch_news()
    print(news_data)  # This will be captured by the C# application
