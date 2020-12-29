﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1062:In externally visible method 'Task<TEntity> BaseOwnedService<TEntity>.CreateAsync(Guid userId, TEntity entity, CancellationToken cancellationToken = default(CancellationToken))', validate parameter 'entity' is non-null before using it. If appropriate, throw an ArgumentNullException when the argument is null or add a Code Contract precondition asserting non-null argument.", Justification = "<Pending>", Scope = "member", Target = "~M:DrifterApps.Holefeeder.Business.BaseOwnedService`1.CreateAsync(System.String,`0,System.Threading.CancellationToken)~System.Threading.Tasks.Task{`0}")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Build", "CA1062:In externally visible method 'Task BaseService<TEntity>.UpdateAsync(string id, TEntity entity, CancellationToken cancellationToken = default(CancellationToken))', validate parameter 'entity' is non-null before using it. If appropriate, throw an ArgumentNullException when the argument is null or add a Code Contract precondition asserting non-null argument.", Justification = "<Pending>", Scope = "member", Target = "~M:DrifterApps.Holefeeder.Business.BaseService`1.UpdateAsync(System.String,`0,System.Threading.CancellationToken)~System.Threading.Tasks.Task")]