import axios from 'axios';
import { titleCase } from 'title-case';

// Preserve non-Latin, human-written titles instead of forcing English casing.
if (/[^\x00-\x7F]/.test(process.env.PR_TITLE || '')) {
    console.log('Non-Latin PR title; leaving it unchanged.');
    process.exit(0);
}

if (process.env.GITHUB_TOKEN) axios.defaults.headers.common['Authorization'] = `token ${process.env.GITHUB_TOKEN}`;
else throw new Error('BOT_TOKEN was not provided in repository secrets or GITHUB_TOKEN was not set correctly.');


// Get PR title
let prTitle = await axios.get(`https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/pulls/${process.env.PR_NUMBER}`)
    .then(res => res.data.title);

// Title case PR title
console.log(`Old PR title: ${prTitle}`);
prTitle = titleCase(prTitle);
console.log(`New PR title: ${prTitle}`);

// Update PR title
await axios.patch(`https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/pulls/${process.env.PR_NUMBER}`,
    { title: prTitle });
