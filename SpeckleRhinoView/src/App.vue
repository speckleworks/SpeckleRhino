<template>
  <div id="app">
    <md-toolbar class="md-large xxxmd-primary">
      <h2 class="md-title" style="flex: 1">Speckle</h2>
      <md-button class="md-icon-button" @click.native='showDev()'>
        <md-icon>settings</md-icon>
      </md-button>
    </md-toolbar>
    <md-tabs class='md-accent'>
      <md-tab id='streams' md-label='streams'>
        <h1 class='md-display-3'>Hello Streams.</h1>
      </md-tab>
      <md-tab id='accounts' md-label='accounts'>
        <accounts-manager></accounts-manager>
      </md-tab>
    </md-tabs>
  </div>
</template>

<script>
import AccountsManager from './components/AccountsManager.vue'

export default {
  name: 'app',
  components: {
    AccountsManager
  },
  data () {
    return {
      msg: 'Welcome to Your Vue.js App'
    }
  },
  methods: {
    showDev( ) {
      // alert("woot")
      Interop.showDev()
    }
  }
}
</script>

<style lang="scss">
#app {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
</style>
