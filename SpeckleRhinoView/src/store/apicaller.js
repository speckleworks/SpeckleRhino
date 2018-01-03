import Axios from 'axios'

export default {
  getStreams( account ) {
    return new Promise( ( resolve, reject ) => {
      if ( !account ) return reject( 'No account provided' )
      Axios.get( account.restApi + '/accounts/streams', { headers: { 'Authorization': account.apiToken } } )
        .then( res => {
          resolve( res.data )
        } )
        .catch( err => {
          reject( err )
        } )
    } )
  },

  registerAccount( data ) {
    return new Promise( ( resolve, reject ) => {
      Axios.post( data.serverUrl + '/accounts/register', {
          email: data.userEmail,
          password: data.password,
          name: data.userName,
          surname: data.userSurname
        } )
        .then( res => {
          resolve( res )
        } )
        .catch( err => {
          // if ( err.response ) {
          //   // The request was made and the server responded with a status code
          //   // that falls out of the range of 2xx
          //   console.log( err.response.data );
          //   console.log( err.response.status );
          //   console.log( err.response.headers );
          // } else if ( err.request ) {
          //   // The request was made but no response was received
          //   // `err.request` is an instance of XMLHttpRequest in the browser and an instance of
          //   // http.ClientRequest in node.js
          //   console.log( err.request );
          // } else {
          //   // Something happened in setting up the request that triggered an err
          //   console.log( 'err', err.message );
          // }
          // console.log( err.config );
          reject( new Error( err.response.data.message ? err.response.data.message : err.message ) )
        } )
    } )
  }

}