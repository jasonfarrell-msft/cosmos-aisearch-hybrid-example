
import os
from typing import Dict, List, Any
from openai import AzureOpenAI

client = AzureOpenAI(
    api_key=os.environ["AZURE_OPENAI_API_KEY"],
    api_version="2024-12-01-preview",
    azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"]
)

def create_response(results: List[Dict[str, Any]]) -> str:
    response = client.chat.completions.create(
        model="gpt5-nano-deployment",
        messages=[
            {"role": "system", "content": "You are a helpful assistant that summarizes search results."},
            {"role": "user", "content": f"""
             Please summarize these search results:
             -----------------------------------------
             {results}
             -----------------------------------------
             
             Return only the summary and no additional context or information"""}
        ]
    )
    return response.choices[0].message.content or "No response"