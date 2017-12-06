<template>
  <div id="app">
    <v-app dark>
      <v-tabs fixed centered v-model='active'>
        <v-tabs-bar class='transparent' dark>
          <v-tabs-item key='Clients' href='Clients'>
            Clients
          </v-tabs-item>
          <v-tabs-item key='accounts' href='accounts'>
            Accounts
          </v-tabs-item>
          <v-tabs-slider color='light-blue'></v-tabs-slider>
        </v-tabs-bar>
        <v-tabs-items>
          <v-tabs-content id='Clients' key='Clients'>
            <v-card flat>
              <client-manager></client-manager>
            </v-card>
          </v-tabs-content>        
          <v-tabs-content id='accounts' key='accounts'>
            <v-card flat>
              <accounts-manager></accounts-manager>
            </v-card>
          </v-tabs-content>        
        </v-tabs-items>
      </v-tabs>
      <v-speed-dial v-model='fab' hover fixed bottom right direction='top'>
        <v-btn slot='activator' fab v-model='fab' color='light-blue'>
          <v-icon>add</v-icon>
          <v-icon>close</v-icon>
        </v-btn>
        <v-btn fab dark small color='cyan' @click='addReceiver'>
          <v-icon>cloud_download</v-icon>
        </v-btn>
        <v-btn fab dark small color='grey' @click='purgeClients'>
          <v-icon>refresh</v-icon>
        </v-btn>
        <v-btn fab dark small color='black white--text' @click='showDev'>
          <v-icon>code</v-icon>
        </v-btn>
      </v-speed-dial>
    </v-app>
  </div>
</template>

<script>
import AccountsManager from './components/AccountsManager.vue'
import ClientManager from './components/ClientManager.vue'
import { EventBus } from './event-bus'

export default {
  name: 'app',
  components: {
    AccountsManager,
    ClientManager
  },
  data () {
    return {
      fab: {},
      active: null
    }
  },
  methods: {
    showDev( ) {
      Interop.showDev()
    },
    addReceiver() {
      EventBus.$emit('show-add-receiver-dialog')
    },
    saveClients() {
      Interop.saveFileClients()
    },
    readClients() {
      Interop.getFileStreams()
    },
    purgeClients() {
      Interop.removeAllClients()
      .then( res => {})
      .catch( res => {})
    }
  }
}
</script>

<style>
body{
}
#app {
}
</style>
