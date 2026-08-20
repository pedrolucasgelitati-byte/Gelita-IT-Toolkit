document.querySelector('.menu')?.addEventListener('click',()=>document.querySelector('.sidebar')?.classList.toggle('open'));
const applyInventoryFilters=()=>{
  const query=(document.querySelector('#machine-search')?.value??'').toLocaleLowerCase('pt-BR').trim();
  const unit=(document.querySelector('#unit-filter')?.value??'').toLocaleLowerCase('pt-BR');
  document.querySelectorAll('#machine-rows tr[data-machine]').forEach(row=>{
    const matchesQuery=query.length===0||`${row.dataset.machine} ${row.dataset.unit} ${row.dataset.version}`.toLocaleLowerCase('pt-BR').includes(query);
    const matchesUnit=unit.length===0||(row.dataset.unit??'').toLocaleLowerCase('pt-BR')===unit;
    row.hidden=!(matchesQuery&&matchesUnit);
  });
};
document.querySelector('#machine-search')?.addEventListener('input',applyInventoryFilters);
document.querySelector('#unit-filter')?.addEventListener('change',applyInventoryFilters);
