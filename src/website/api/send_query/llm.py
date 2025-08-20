
import os
from typing import Dict, List, Any
from openai import AzureOpenAI

from dotenv import load_dotenv
load_dotenv()

client = AzureOpenAI(
    api_key=os.environ["AZURE_OPENAI_API_KEY"],
    api_version="2024-12-01-preview",
    azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"]
)

def create_response(results: List[Dict[str, Any]]) -> str:
    response = client.chat.completions.create(
        model="gpt-5-nano-deployment",
        messages=[
            {"role": "system", "content": "You are a helpful assistant that summarizes search results into natural language as part of a chat experience. Do not use lists.."},
            {"role": "user", "content": f"""
             Please summarize these search results:
             -----------------------------------------
             {results}
             -----------------------------------------

             Avoid lists when responding.
             Include the name, company, and contact information for the person giving the answer.
             Place a newline after each user specific response.
             Format this for an HTML response. Names should have a strong tag around the content.
             
             Example:
             <strong>Maurice Quinn</strong> — MidAmerican Energy</strong><br />
             Email: maurice.quinn@midamerican.com | Phone: 7022098303<br />
             Answer: All BHE entities use the supplier's W9 to determine state residency, and accounting charge codes are used to determine where project work was performed.
             <br />&nbsp;
             """}
        ]
    )
    return response.choices[0].message.content or "No response"