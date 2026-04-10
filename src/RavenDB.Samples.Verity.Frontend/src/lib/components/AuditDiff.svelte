<script lang="ts">
  import { diffWords } from 'diff';
  import type { AuditRevision } from '$lib/services/audit';

  interface Props {
    // older revision (the one being viewed)
    from: AuditRevision;
    // newer revision (what it became)
    to: AuditRevision;
  }

  let { from, to }: Props = $props();

  interface WordPart {
    value: string;
    added?: boolean;
    removed?: boolean;
  }

  function wordDiff(oldText: string, newText: string): WordPart[] {
    return diffWords(oldText, newText);
  }

  const noteDiff    = $derived(wordDiff(from.data.auditString, to.data.auditString));
  const noteChanged = $derived(noteDiff.some(p => p.added || p.removed));
</script>

<div class="diff-root">
  {#if noteChanged}
    <p class="diff-text">
      {#each noteDiff as part}
        {#if part.removed}
          <span class="removed">{part.value}</span>
        {:else if part.added}
          <span class="added">{part.value}</span>
        {:else}
          <span>{part.value}</span>
        {/if}
      {/each}
    </p>
  {:else}
    <p class="no-changes">No changes in notes for this revision.</p>
  {/if}
</div>

<style>
  .diff-root {
    display: flex;
    flex-direction: column;
  }

  .diff-text {
    margin: 0;
    line-height: 1.8;
    font-size: 0.95rem;
    color: #9ab4cc;
    white-space: pre-wrap;
    background: #0d1825;
    border-radius: 8px;
    padding: 0.75rem 1rem;
  }

  .removed {
    background: #3a1010;
    color: #f07070;
    text-decoration: line-through;
    border-radius: 2px;
    padding: 0 1px;
  }

  .added {
    background: #0d2a18;
    color: #5de89a;
    border-radius: 2px;
    padding: 0 1px;
  }

  .no-changes {
    margin: 0;
    font-size: 0.875rem;
    color: #4a6a88;
    font-style: italic;
  }
</style>
