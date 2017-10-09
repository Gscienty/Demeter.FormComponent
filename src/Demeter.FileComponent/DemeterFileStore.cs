using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Demeter.FormComponent;
using MongoDB.Driver;

namespace Demeter.FileComponent
{
    public class DemeterFileStore<TFile> : IFormStore<TFile>
        where TFile : DemeterFile, new()
    {
        private readonly IMongoCollection<TFile> _formCollection;
        private readonly string _baseFolderPath;

        public DemeterFileStore(
            IMongoDatabase database,
            string formsCollection,
            string folderPath)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if (formsCollection == null)
            {
                throw new ArgumentNullException(nameof(formsCollection));
            }
            this._formCollection = database.GetCollection<TFile>(formsCollection);
            this._baseFolderPath = folderPath;
        }

        Task<FormResult> IFormStore<TFile>.CreateAsync(TFile form, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        Task<FormResult> IFormStore<TFile>.DeleteAsync(TFile form, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        Task<TFile> IFormStore<TFile>.FindByIdAsync(string id, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        Task<IEnumerable<TFile>> IFormStore<TFile>.LastestAsync(int count, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        Task<IEnumerable<TFile>> IFormStore<TFile>.QueryAsync(string queryString, int count, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        Task<FormResult> IFormStore<TFile>.UpdateAsync(TFile form, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
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
        // ~DemeterFileStore() {
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