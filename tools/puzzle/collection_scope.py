"""Validate the observed authorized channel and date range before downloading attachments."""
import re
from datetime import date, datetime


def allowed_timestamp(timestamp, minimum_date):
    if not timestamp:
        raise ValueError('Attachment message has no timestamp')
    if minimum_date is None:
        return True
    return datetime.fromisoformat(timestamp.replace('Z', '+00:00')).date() >= date.fromisoformat(minimum_date)


def observed_scope(session, channel, minimum_date):
    query = session.evaluate("document.querySelector('[role=combobox][contenteditable=true]')?.textContent")
    if not query or not re.search(r'\bin:\s*' + re.escape(channel) + r'\b', query) or 'stop there,' not in query:
        raise ValueError('The observed search does not match the authorized channel')
    links = session.evaluate("Array.from(document.querySelectorAll('a[href^=\"/channels/555971084415926272/\"]')).map(a=>({text:a.textContent,href:a.getAttribute('href')}))")
    matching = [link for link in links if channel in link['text']]
    identifiers = {link['href'].split('/')[3] for link in matching}
    if len(identifiers) != 1:
        raise ValueError('The requested channel could not be uniquely identified in the observed server')
    return dict(query=query, channel=channel, channelId=identifiers.pop(), minimumDate=minimum_date)
