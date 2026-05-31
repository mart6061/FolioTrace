<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';

  type ToDoState = 'Core' | 'Ideas' | 'In Progress' | 'Done';
  type ToDoCard = {
    title: string;
    state: ToDoState;
    lines?: string[];
    text?: string;
  };

  const cards: ToDoCard[] = [
    {
      title: 'Simple Valuation',
      state: 'In Progress',
      lines: [
        'Asset list, cash list and totals',
        'Some grouping by Asset / CFI',
        'Followed by cash',
        'Totals',
        'Debt',
        'Secondary totals'
      ]
    },
    {
      title: 'Authentication',
      state: 'Core',
      text: 'Using WorkOS Auth to manage JWT management, plus UI to API and DB secrets?'
    },
    {
      title: 'Valuation Detailed Status',
      state: 'Core',
      lines: [
        'Cover',
        'Index',
        'Intro',
        'Asset Class Level Valuation',
        'Holding Level Valuation',
        'Asset Class Transactions',
        'Holding Level Transaction',
        'Cash Accounts',
        'Debt Accounts',
        'Management Report'
      ]
    }
  ];

  function stateClass(state: ToDoState) {
    return state.toLowerCase().replace(/\s+/g, '-');
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Planning</p>
      <div class="page-title-row">
        <h1 class="page-title">To Do</h1>
        <BookmarkButton />
      </div>
      <p class="page-subtitle">Current product and technical priorities.</p>
    </div>
  </section>

  <section class="page-container page-section">
    <div class="todo-list">
      {#each cards as card}
        <article class={`todo-card todo-card-${stateClass(card.state)}`}>
          <div class="todo-card-header">
            <h2 class="todo-card-title">{card.title}</h2>
            <span class="todo-card-state">State: {card.state}</span>
          </div>
          <div class="todo-card-body">
            {#if card.text}
              <p>{card.text}</p>
            {/if}

            {#if card.lines}
              <ul>
                {#each card.lines as line}
                  <li>{line}</li>
                {/each}
              </ul>
            {/if}
          </div>
        </article>
      {/each}
    </div>
  </section>
</main>
