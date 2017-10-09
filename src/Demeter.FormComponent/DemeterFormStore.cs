using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Demeter.FormComponent
{
    public sealed class DemeterFormStore<TForm> : IFormStore<TForm>
        where TForm : DemeterForm, new()
    {
        private readonly IMongoCollection<TForm> _formCollection;

        public DemeterFormStore(IMongoDatabase database, string formsCollection)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if (formsCollection == null)
            {
                throw new ArgumentNullException(nameof(formsCollection));
            }

            this._formCollection = database.GetCollection<TForm>(formsCollection);
        }

        async Task<FormResult> IFormStore<TForm>.CreateAsync(TForm form,
            CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            await this._formCollection.InsertOneAsync(form, cancellationToken: cancellationToken);
            return FormResult.Success;
        }

        async Task<FormResult> IFormStore<TForm>.DeleteAsync(TForm form,
            CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            cancellationToken.ThrowIfCancellationRequested();
            form.Delete();

            var query = Builders<TForm>.Filter.Eq(f => f.Id, form.Id);
            var update = Builders<TForm>.Update.Set(f => f.DeleteOn, form.DeleteOn);

            var result = await this._formCollection.UpdateOneAsync(
                query,
                update,
                new UpdateOptions { IsUpsert = false },
                cancellationToken);
            return result.IsModifiedCountAvailable && result.ModifiedCount == 1
                ? FormResult.Success
                : FormResult.Failed();
        }

        async Task<TForm> IFormStore<TForm>.FindByIdAsync(string id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var query = Builders<TForm>.Filter.And(
                Builders<TForm>.Filter.Eq(f => f.Id, id),
                Builders<TForm>.Filter.Eq(f => f.DeleteOn, null)
            );

            return await this._formCollection.Find(query).FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        async Task<IEnumerable<TForm>> IFormStore<TForm>.LastestAsync(int count, CancellationToken cancellationToken)
        {
            var query = Builders<TForm>.Filter.Eq(f => f.DeleteOn, null);

            return await this._formCollection
                .Find(query)
                .SortByDescending(f => f.CreateOn)
                .Limit(count)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        }

        Task<IEnumerable<TForm>> IFormStore<TForm>.QueryAsync(string queryString, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        async Task<FormResult> IFormStore<TForm>.UpdateAsync(TForm form, CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            var query = Builders<TForm>.Filter.And(
                Builders<TForm>.Filter.Eq(f => f.DeleteOn, null),
                Builders<TForm>.Filter.Eq(f => f.Id, form.Id)
            );
            
            var result = await this._formCollection.ReplaceOneAsync(
                query,
                form,
                new UpdateOptions { IsUpsert = false },
                cancellationToken);

            return result.IsModifiedCountAvailable && result.ModifiedCount == 1
                ? FormResult.Success
                : FormResult.Failed();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DemeterFormStore() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void System.IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}