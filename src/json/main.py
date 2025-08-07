import pandas as pd
import json

# Read the Excel file
df = pd.read_excel('survey1.xlsx', engine='openpyxl')


# Replace NaT/NaN with None
df = df.where(pd.notnull(df), None)

# Force RespondentId and CollectorId to string, fill NaN with empty string
for col in ['Respondent Id', 'Collector Id']:
    if col in df.columns:
        df[col] = df[col].astype(str).replace('nan', '')

# Force Start Date and End Date to ISO datetime string, fill NaT with empty string
for col in ['Start Date', 'End Date']:
    if col in df.columns:
        df[col] = pd.to_datetime(df[col], errors='coerce').dt.strftime('%Y-%m-%dT%H:%M:%S').replace('NaT', '')

# Ensure First Name, Last Name, Email Address are normal strings, fill NaN with empty string
for col in ['First Name', 'Last Name', 'Email Address']:
    if col in df.columns:
        df[col] = df[col].astype(str).replace('nan', '')

data = df.to_dict(orient='records')

# Write to JSON file
with open('survey.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
