
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
            {"role": "system", "content": "You are a helpful assistant that summarizes search results into natural language as part of a chat experience. Do not use lists."},
            {"role": "user", "content": f"""
                Here are results from a search that has been conducted:
                -----------------------------------------
                {results}
                -----------------------------------------

                First group results with the same Name and Companny
                Then summarize those grouped results

                Format each result so that:
                - Name (with <strong> tag) and company should be on a single line
                  Email and Phone are on their own line, with no extra line spacing between them.
                  Summarize all answers in the group into a summary of no more than two sentences on its own line.
                - There should be a single empty line (use <br /> or &nbsp;) between individual results, but no extra spacing within a result.
                - Do not use lists or paragraphs.
                - Example:
                <div><strong>Maurice Quinn</strong> - MidAmerican Energy</div>
                <div>maurice.quinn@midamerican.com | Phone: 7022098303</div>
                <div>All BHE entities use the supplier's W9 to determine state residency, and accounting charge codes are used to determine where project work was performed.</div>
                <br />

                -----------------------------------------------
                """}
        ]
    )
    return response.choices[0].message.content or "No response"