# Arquitetura do Projeto - G3Plugins

## Objetivo

O G3Plugins é um conjunto de plugins para Autodesk Revit desenvolvido em C# utilizando a Revit API 2025 e .NET Framework 4.8.

A arquitetura foi organizada para manter baixo acoplamento, alta reutilização de código e facilidade de manutenção.

Cada camada possui uma única responsabilidade.

---

# Estrutura

```
G3Plugins
│
├── Application
├── Ribbon
├── Commands
├── GeometryServices
├── DimensionServices
├── Models
├── Utils
```

---

# Responsabilidades

## Application

Responsável pela inicialização do plugin.

Funções:

- Registrar o Add-in.
- Inicializar o Ribbon.
- Não contém regras de negócio.

---

## Ribbon

Responsável por criar a interface do Revit.

Funções:

- Criar abas.
- Criar painéis.
- Criar botões.

Não deve conter nenhuma lógica de negócio.

---

## Commands

Cada comando representa uma ação iniciada pelo usuário.

Responsabilidades:

- Receber o contexto do Revit.
- Validar pré-condições.
- Chamar os serviços necessários.

Commands nunca devem implementar regras complexas.

---

## GeometryServices

Camada responsável por toda a lógica geométrica.

Exemplos:

- Encontrar paredes.
- Encontrar alinhamentos.
- Detectar portas.
- Detectar janelas.
- Encontrar faces.
- Encontrar referências.
- Ordenar elementos geometricamente.

Toda lógica relacionada à geometria deve ficar nesta camada.

---

## DimensionServices

Responsável exclusivamente pela criação de cotas.

Exemplos:

- Criar Dimension.
- Criar cadeias de cotas.
- Criar cotas totais.
- Definir offsets.
- Criar ReferenceArray.

Não deve localizar elementos da modelagem.

Recebe referências já processadas pela GeometryServices.

---

## Models

Classes de domínio.

Armazenam informações utilizadas entre serviços.

Exemplos:

- WallAlignment
- OpeningInfo
- DimensionData

Não devem acessar diretamente a API do Revit para executar operações.

---

## Utils

Funções auxiliares reutilizáveis.

Exemplos:

- Conversão de unidades.
- Métodos matemáticos.
- Ordenação.
- Extensões.

Não devem conter regras específicas do domínio.

---

# Fluxo esperado

```
Usuário

↓

Ribbon

↓

Command

↓

GeometryServices

↓

DimensionServices

↓

Revit API
```

---

# Princípios

O projeto deve seguir:

- SOLID
- Baixo acoplamento
- Alta coesão
- Código reutilizável
- Métodos pequenos
- Separação clara de responsabilidades

Sempre reutilizar serviços existentes antes de criar novos.

Evitar duplicação de código.

---

# Objetivo das próximas implementações

Novas funcionalidades devem respeitar esta arquitetura.

Sempre que possível:

- localizar informações na GeometryServices;
- criar elementos do Revit na DimensionServices;
- manter Commands simples;
- manter Models apenas como estruturas de dados.

A arquitetura deve permanecer escalável para futuras funcionalidades sem necessidade de grandes refatorações.