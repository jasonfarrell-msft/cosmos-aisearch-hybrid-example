"""
Answer generation module for processing user queries.

This module contains functions for generating answers based on user queries using Azure AI Search.
"""

import logging
import os

from typing import Optional, List, Dict, Any
from azure.core.credentials import AzureKeyCredential
from azure.search.documents import SearchClient

credential = AzureKeyCredential(os.environ["AZURE_SEARCH_KEY"])
search_client = SearchClient(
    endpoint=os.environ["AZURE_SEARCH_ENDPOINT"],
    index_name=os.environ["AZURE_SEARCH_INDEX"],
    credential=credential
)

def _search_documents(query: str, top_k: int = 5) -> List[Dict[str, Any]]:
    """
    Search for relevant documents using Azure AI Search.
    
    Args:
        query (str): The search query
        top_k (int): Number of top results to return (default: 5)
        
    Returns:
        List[Dict[str, Any]]: List of search results
        
    Raises:
        Exception: If search operation fails
    """
    results = search_client.search(
        search_text=query,
        top=top_k,
        include_total_count=True
    )
    
    documents = []
    for result in results:
        documents.append(dict(result))
    
    return documents


def answer_query(query: str) -> str:
    results = _search_documents(query)

    return f"You search returned: {results}"
